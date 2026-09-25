using DD2ModConfig.Code.Attributes;
using DD2ModConfig.Code.Core;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using System.Reflection;

namespace DD2ModConfig.Code.ResoluteOrMeltdown;

public static class RMRegistry
{
    private static readonly object _lock = new();
    private static List<RMRegistration>? _cache;

    public static IReadOnlyList<RMRegistration> All
    {
        get
        {
            EnsureScanned();
            return _cache!;
        }
    }

    public static IReadOnlyList<RMRegistration> Query(Func<RMRegistration, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        EnsureScanned();
        return _cache!.Where(predicate)
            .OrderBy(r => r.CharacterId, StringComparer.Ordinal)
            .ThenBy(r => r.ModelType.FullName, StringComparer.Ordinal)
            .ToList();
        //确保每次查询顺序一致
    }

    public static IReadOnlyList<RMRegistration> GetUniqueList(string CharacterId)
    {
        EnsureScanned();
        return _cache!.Where(r => r.IsUnique && r.CharacterId == CharacterId).ToList();
    }

    private static void EnsureScanned()
    {
        if (_cache != null) return;

        lock (_lock)
        {
            if (_cache != null) return;
            _cache = ScanAll();
            Log.Info($"[RMRegistry] Scanned {_cache.Count} RM registrations.");
        }
    }

    private static List<RMRegistration> ScanAll()
    {
        var result = new List<RMRegistration>();

        IEnumerable<Type> types;
        try
        {
            types = ReflectionHelper.GetSubtypesInMods<ResoluteOrMeltdownModel>();
        }
        catch (Exception e)
        {
            Log.Error($"[RMRegistry] Failed to enumerate RM subtypes: {e}");
            return result;
        }

        foreach (var type in types)
        {
            try
            {
                if (type.IsAbstract) continue;
                if (!typeof(ResoluteOrMeltdownModel).IsAssignableFrom(type)) continue;

                var attr = type.GetCustomAttribute<RMRegisterAttribute>();
                if (attr == null) continue;

                var characterId = attr.CharacterId;
                if (string.IsNullOrEmpty(characterId))
                {
                    Log.Error($"[RMRegistry] {type.FullName} has RMAttribute " +
                              $"but CharacterId resolved to empty. Skipped.");
                    continue;
                }
                var factory = () => RMModelDb.ResoluteOrMeltdown(type);
                result.Add(new RMRegistration(type, characterId, attr.RMType, attr.IsUnique, factory));
            }
            catch (Exception e)
            {
                Log.Error($"[RMRegistry] Failed to scan type {type.FullName}: {e}");
            }
        }

        return result;
    }
}