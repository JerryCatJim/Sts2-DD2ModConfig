using BaseLib.Config;
using DD2ModConfig.Code.Config;
using DD2ModConfig.Code.Core;
using DD2ModConfig.Code.VFX;
using Godot;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using System.Reflection;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace DD2ModConfig.Code;

/**
 * Ideas
 * 
 * Self Bind
 * 
 * Bind effect - square texture based on model size, lines random generated (amount equal to bind amount)
 * shader of transparency of line based on average of point spread of the model
 * colored
 * 
 * Bind... rename? Necrobinder kinda overlaps.
 * */

[ModInitializer(nameof(Initialize))]
public partial class MainFile// : Node
{
	public const string ModId = "DD2ModConfig"; //At the moment, this is used only for the Logger and harmony names.

    public static Logger Logger { get; } =
		new(ModId, LogType.Generic);

	public static void Initialize()
	{
		RMSubscriber.Subscribe();
        PreloadEffectScenes();

        ModConfigRegistry.Register(ModId, new DD2ModConfigCfg());

        Harmony harmony = new(ModId);
        var assembly = Assembly.GetExecutingAssembly();
        ScriptManagerBridge.LookupScriptsInAssembly(assembly);
        harmony.PatchAll();
        //输出所有检测到的Patch类
        /*var patchTypes = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<HarmonyPatch>() != null)
                .ToList();
        Log.Info($"Found {patchTypes.Count} types with [HarmonyPatch].");
        foreach (var t in patchTypes)
        {
            Log.Info($"  - {t.FullName}");
        }

        try
        {
            harmony.PatchAll();
            Log.Info("Harmony PatchAll completed successfully.");
        }
        catch (Exception ex)
        {
            Log.Info($"Harmony PatchAll FAILED: {ex}");
        }*/
    }
    private static void PreloadEffectScenes()
    {
        var paths = new List<string> {
            "res://DD2ModConfig/Scenes/DD2Scenes/StressUp.tscn",
            "res://DD2ModConfig/Scenes/DD2Scenes/StressDown.tscn",
            "res://DD2ModConfig/Scenes/DD2Scenes/DeathArmor.tscn",
            "res://DD2ModConfig/Scenes/DD2Scenes/DeathBlow.tscn",
            "res://DD2ModConfig/Scenes/DD2Scenes/DeathDoor.tscn",
        };
        foreach (var path in paths)
        {
            if (VFXUtil.ModSceneCache.ContainsKey(path)) continue;
            var scene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Reuse);
            if (scene != null)
            {
                VFXUtil.ModSceneCache[path] = scene;
            }
        }
    }
}