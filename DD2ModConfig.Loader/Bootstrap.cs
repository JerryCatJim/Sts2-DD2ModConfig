using System.Reflection;
using System.Runtime.Loader;
using HarmonyLib;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;

namespace DD2ModConfig.Loader;

[ModInitializer(nameof(Initialize))]
public static class Bootstrap
{
    private const string ModId = "DD2ModConfig";
    private const string RealDllName = "DD2ModConfig.dll";

    // Hardcoded variant versions (newest first for scanning)
    private static readonly string[] KnownVersions = ["0.114.0", "0.113.0", "0.111.0"];

    private static readonly Lock VariantAssembliesLock = new();
    private static readonly List<Assembly> VariantAssemblies = [];
    private static bool _reflectionBridgePatched;

    public static void Initialize()
    {
        var loaderDir = Path.GetDirectoryName(typeof(Bootstrap).Assembly.Location);
        if (string.IsNullOrEmpty(loaderDir))
        {
            Log.Error("[DD2ModConfig.Loader] Could not resolve loader directory.");
            return;
        }

        var libRoot = Path.Combine(loaderDir, "lib");
        if (!Directory.Exists(libRoot))
        {
            Log.Error($"[DD2ModConfig.Loader] Missing lib directory: {libRoot}");
            return;
        }

        var hostVersion = DetectHostVersion();
        var picked = PickVariant(loaderDir, libRoot, hostVersion);
        if (picked is null)
        {
            Log.Error($"[DD2ModConfig.Loader] No compatible variant under {libRoot} (host={hostVersion?.ToString() ?? "unknown"}).");
            return;
        }

        Log.Info($"[DD2ModConfig.Loader] Host version {hostVersion}; picked variant {picked.CompatTarget}.");

        var realDll = picked.DllPath;
        if (!File.Exists(realDll))
        {
            Log.Error($"[DD2ModConfig.Loader] Variant folder missing {RealDllName}: {realDll}");
            return;
        }

        var alc = AssemblyLoadContext.GetLoadContext(typeof(Bootstrap).Assembly) ?? AssemblyLoadContext.Default;
        Assembly realAsm;
        try
        {
            realAsm = alc.LoadFromAssemblyPath(realDll);
            RegisterVariantAssembly(realAsm);
        }
        catch (Exception ex)
        {
            Log.Error($"[DD2ModConfig.Loader] Failed to load {realDll}: {ex}");
            return;
        }

        try
        {
            InvokeRealInitializer(realAsm);
        }
        catch (Exception ex)
        {
            Log.Error($"[DD2ModConfig.Loader] Failed to initialize DD2ModConfig: {ex}");
        }

        // Register this assembly's C# scripts with Godot's ScriptManager.
        // Without this, Godot cannot find classes like NBalatroInspectScreen
        // because the variant DLL is loaded via AssemblyLoadContext, bypassing
        // Godot's normal assembly-scan pipeline.
        try
        {
            EnsureGodotScriptsRegistered(realAsm);
        }
        catch (Exception ex)
        {
            Log.Warn($"[DD2ModConfig.Loader] EnsureGodotScriptsRegistered failed: {ex.Message}");
        }

        // Ensure mod model entries are in the serialization cache.
        // ModelIdSerializationCache.Init() may have already run before our
        // variant DLL was loaded; this rebuild catches that case.
        try
        {
            ModelIdSerializationCacheRebuildPatch.TryRebuild();
        }
        catch (Exception ex)
        {
            Log.Warn($"[DD2ModConfig.Loader] ModelIdSerializationCache rebuild failed: {ex.Message}");
        }
    }

    private static Version? DetectHostVersion()
    {
        // Try ReleaseInfo first (may have "v" prefix like "v0.110.0")
        try
        {
            var ri = ReleaseInfoManager.Instance.ReleaseInfo;
            var version = ri?.Version;
            Log.Info($"[DD2ModConfig.Loader] ReleaseInfo.Version raw: '{version ?? "NULL"}'");
            if (!string.IsNullOrWhiteSpace(version))
            {
                var trimmed = version.StartsWith('v') || version.StartsWith('V')
                    ? version[1..] : version;
                Log.Info($"[DD2ModConfig.Loader] Trimmed: '{trimmed}'");
                if (Version.TryParse(trimmed, out var v))
                {
                    Log.Info($"[DD2ModConfig.Loader] Parsed version: {v}");
                    return v;
                }
                Log.Warn($"[DD2ModConfig.Loader] Failed to parse version: '{trimmed}'");
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"[DD2ModConfig.Loader] ReleaseInfo lookup failed: {ex.Message}");
        }

        // Fall back to sts2 assembly version
        var av = typeof(ModManager).Assembly.GetName().Version;
        Log.Info($"[DD2ModConfig.Loader] Fallback assembly version: {av}");
        if (av != null && !(av.Major == 0 && av.Minor == 0 && av.Build == 0))
            return av;

        return null;
    }

    /// <summary>
    /// Scans lib/ for variant DLLs and picks the best match for the host version.
    /// No JSON manifest needed — just looks for lib/&lt;version&gt;/DD2ModConfig.dll.
    /// </summary>
    private static VariantCandidate? PickVariant(string loaderDir, string libRoot, Version? host)
    {
        var variants = new List<VariantCandidate>();

        foreach (var versionStr in KnownVersions)
        {
            if (!Version.TryParse(versionStr, out var version))
                continue;

            var variantDir = Path.Combine(libRoot, versionStr);
            var dllPath = Path.Combine(variantDir, RealDllName);
            if (File.Exists(dllPath))
                variants.Add(new VariantCandidate(versionStr, version, dllPath));
        }

        if (variants.Count == 0)
        {
            Log.Error($"[DD2ModConfig.Loader] No variant DLLs found under {libRoot}.");
            return null;
        }

        // Sort ascending so we can pick highest <= host
        variants.Sort((a, b) => a.Version.CompareTo(b.Version));

        if (host is null)
        {
            Log.Info("[DD2ModConfig.Loader] Host version unknown; using newest bundled variant.");
            return variants[^1];
        }

        // Pick the highest version that is <= host
        var candidates = variants.Where(x => x.Version <= host).ToList();
        if (candidates.Count > 0)
            return candidates[^1];

        Log.Info($"[DD2ModConfig.Loader] No variant <= host {host}; using newest as fallback.");
        return variants[^1];
    }

    /// <summary>
    /// Registers the variant assembly so that ModelDb.Init() can discover our card/cardpool models.
    /// v0.110.0+ has ModManager.AssociateAssemblyWithMod.
    /// v0.107 does NOT — we fall back to setting Mod.assembly directly via reflection.
    /// NOTE: All Mod field access must use reflection (not direct dot-access) because
    /// the field name changed between versions (assembly→assemblies), and MonoMod will
    /// throw MissingFieldException at JIT time for direct field references.
    /// </summary>
    private static void RegisterVariantAssembly(Assembly variantAssembly)
    {
        var registered = false;

        // Try v0.110.0+ API first
        try
        {
            var method = typeof(ModManager).GetMethod("AssociateAssemblyWithMod",
                BindingFlags.Public | BindingFlags.Static, null,
                [typeof(string), typeof(Assembly)], null);
            if (method != null)
            {
                method.Invoke(null, [ModId, variantAssembly]);
                Log.Info("[DD2ModConfig.Loader] Registered via AssociateAssemblyWithMod (v0.110.0+).");
                registered = true;
            }
        }
        catch (Exception ex)
        {
            Log.Info($"[DD2ModConfig.Loader] AssociateAssemblyWithMod failed: {ex.Message}");
        }

        // Fallback for v0.107: set Mod.assembly directly via reflection.
        if (!registered)
        {
            // We use reflection for the assembly field because Mod.assembly was
            // renamed to Mod.assemblies in v0.110.0.
            // Match by manifest.id — mod.assembly is null during init.
            try
            {
                var asmField = typeof(Mod).GetField("assembly",
                    BindingFlags.Public | BindingFlags.Instance);
                if (asmField == null)
                {
                    Log.Warn("[DD2ModConfig.Loader] Mod.assembly field not found (v0.110.0+ renamed it).");
                }
                else
                {
                    foreach (var mod in ModManager.Mods)
                    {
                        if (mod.manifest?.id != ModId)
                            continue;

                        asmField.SetValue(mod, variantAssembly);
                        Log.Info("[DD2ModConfig.Loader] Registered via direct Mod.assembly set (v0.107 fallback).");
                        registered = true;
                        break;
                    }

                    if (!registered)
                        Log.Warn("[DD2ModConfig.Loader] Could not find our Mod entry to register assembly.");
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"[DD2ModConfig.Loader] Fallback registration failed: {ex.Message}");
            }
        }

        // Store variant assembly for ReflectionHelperModTypesPatch
        // and install the Harmony patch that hooks ModTypes to include our types.
        if (registered)
        {
            lock (VariantAssembliesLock)
                VariantAssemblies.Add(variantAssembly);
            EnsureReflectionBridgePatch();
        }
    }

    /// <summary>
    /// Returns ALL types from all loaded variant assemblies.
    /// Called by ReflectionHelperModTypesPatch.Postfix.
    /// ModelDb uses ModTypes to discover ALL AbstractModel subclasses
    /// (cards, enchantments, powers, relics, card pools, etc.), so we must
    /// return everything. InitId failures are now handled by
    /// ModelIdSerializationCacheRebuildPatch instead of swallowing exceptions.
    /// </summary>
    internal static Type[] GetVariantModTypes()
    {
        Assembly[] assemblies;
        lock (VariantAssembliesLock)
            assemblies = [.. VariantAssemblies];

        return assemblies.SelectMany(a =>
        {
            try { return a.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { return ex.Types.OfType<Type>(); }
            catch { return []; }
        }).Distinct().ToArray();
    }

    /// <summary>
    /// Applies Harmony patches from the Loader assembly, including
    /// ReflectionHelperModTypesPatch which ensures ModTypes always
    /// includes types from our variant assemblies.
    /// </summary>
    private static void EnsureReflectionBridgePatch()
    {
        if (_reflectionBridgePatched)
            return;

        var harmony = new Harmony("DD2ModConfig.Loader.ReflectionBridge");
        harmony.PatchAll(typeof(Bootstrap).Assembly);
        _reflectionBridgePatched = true;
        Log.Info("[DD2ModConfig.Loader] Reflection bridge patch installed.");
    }

    private static void InvokeRealInitializer(Assembly realAsm)
    {
        foreach (var t in realAsm.GetTypes())
        {
            var attr = t.GetCustomAttribute<ModInitializerAttribute>();
            if (attr is null) continue;

            var method = t.GetMethod(attr.initializerMethod,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method is null) continue;

            method.Invoke(null, null);
            return;
        }

        Log.Error($"[DD2ModConfig.Loader] No ModInitializer found in {realAsm.FullName}.");
    }

    private sealed record VariantCandidate(string CompatTarget, Version Version, string DllPath);

    /// <summary>
    /// Register C# scripts from the given assembly with Godot's ScriptManager.
    /// The variant DLL is loaded via AssemblyLoadContext, which bypasses Godot's
    /// normal assembly-scan pipeline. We manually call ScriptManagerBridge's
    /// LookupScriptsInAssembly via reflection to force Godot to discover types
    /// decorated with [ScriptPath] (generated by the Godot source generator).
    /// </summary>
    private static void EnsureGodotScriptsRegistered(Assembly assembly)
    {
        // Locate GodotSharp.dll (already loaded in the process)
        var godotSharp = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "GodotSharp");
        if (godotSharp == null)
        {
            Log.Warn("[DD2ModConfig.Loader] GodotSharp not found, skipping script registration.");
            return;
        }

        // Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(Assembly)
        var bridgeType = godotSharp.GetType("Godot.Bridge.ScriptManagerBridge");
        var lookupMethod = bridgeType?.GetMethod(
            "LookupScriptsInAssembly",
            BindingFlags.Public | BindingFlags.Static,
            null, [typeof(Assembly)], null);
        if (bridgeType == null || lookupMethod == null)
        {
            Log.Warn("[DD2ModConfig.Loader] ScriptManagerBridge.LookupScriptsInAssembly not found.");
            return;
        }

        // Check if already registered (read _pathTypeBiMap)
        if (AreDD2ModConfigScriptsRegistered(assembly, bridgeType))
        {
            Log.Info("[DD2ModConfig.Loader] Godot scripts already registered, skipping.");
            return;
        }

        var lookup = lookupMethod.CreateDelegate<Action<Assembly>>();
        lookup(assembly);
        Log.Info($"[DD2ModConfig.Loader] Registered Godot scripts for {assembly.GetName().Name}.");
    }

    /// <summary>
    /// Check whether the assembly's script paths are already known to Godot's
    /// ScriptManager by peeking at its internal _pathTypeBiMap.
    /// </summary>
    private static bool AreDD2ModConfigScriptsRegistered(Assembly assembly, Type bridgeType)
    {
        try
        {
            // Enumerate [ScriptPath] attributes on types that have [AssemblyHasScripts]
            var scriptPaths = EnumerateDD2ModConfigScriptPaths(assembly).ToArray();
            if (scriptPaths.Length == 0)
                return true; // nothing to register

            var pathTypeBiMap = bridgeType.GetField(
                "_pathTypeBiMap",
                BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);
            if (pathTypeBiMap == null)
                return false;

            var tryGetScriptType = pathTypeBiMap.GetType().GetMethod(
                "TryGetScriptType",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, [typeof(string), typeof(Type).MakeByRefType()], null);
            if (tryGetScriptType == null)
                return false;

            var args = new object?[] { null, null };
            foreach (var path in scriptPaths)
            {
                args[0] = path;
                args[1] = null;
                if (tryGetScriptType.Invoke(pathTypeBiMap, args) is not true)
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Enumerate script paths marked with [ScriptPath] attribute on types
    /// declared in assemblies decorated with [AssemblyHasScripts].
    /// These attributes are generated by Godot's source generator at build time.
    /// </summary>
    private static IEnumerable<string> EnumerateDD2ModConfigScriptPaths(Assembly assembly)
    {
        // AssemblyHasScriptsAttribute is defined in GodotSharp
        var asmScriptsAttrType = assembly.GetType("Godot.AssemblyHasScriptsAttribute")
            ?? AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.FullName == "Godot.AssemblyHasScriptsAttribute");

        // ScriptPathAttribute is defined in GodotSharp
        var scriptPathAttrType = assembly.GetType("Godot.ScriptPathAttribute")
            ?? AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.FullName == "Godot.ScriptPathAttribute");

        if (asmScriptsAttrType == null || scriptPathAttrType == null)
            yield break;

        // Check if this assembly has the [AssemblyHasScripts] attribute
        var scriptsAttr = assembly.GetCustomAttributes(asmScriptsAttrType, false)
            .FirstOrDefault();
        if (scriptsAttr == null)
            yield break;

        // Read RequiresLookup property
        var requiresLookupProp = asmScriptsAttrType.GetProperty("RequiresLookup");
        bool requiresLookup = requiresLookupProp != null &&
                              (bool)(requiresLookupProp.GetValue(scriptsAttr) ?? true);

        // Get the Path property from ScriptPathAttribute
        var pathProp = scriptPathAttrType.GetProperty("Path");

        // Determine candidate types
        IEnumerable<Type> candidateTypes;
        if (requiresLookup)
        {
            // GodotObject lives in GodotSharp.dll
            var godotSharp = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "GodotSharp");
            var godotObjectType = godotSharp?.GetType("Godot.GodotObject");
            if (godotObjectType == null)
                yield break;

            candidateTypes = assembly.GetTypes()
                .Where(t => !t.IsNested && godotObjectType.IsAssignableFrom(t));
        }
        else
        {
            var scriptTypesProp = asmScriptsAttrType.GetProperty("ScriptTypes");
            if (scriptTypesProp == null)
                yield break;
            candidateTypes = (IEnumerable<Type>?)scriptTypesProp.GetValue(scriptsAttr) ?? [];
        }

        foreach (var type in candidateTypes)
        {
            var scriptPathAttr = type.GetCustomAttributes(scriptPathAttrType, false)
                .FirstOrDefault();
            if (scriptPathAttr == null)
                continue;

            var path = pathProp?.GetValue(scriptPathAttr) as string;
            if (!string.IsNullOrWhiteSpace(path))
                yield return path;
        }
    }
}
