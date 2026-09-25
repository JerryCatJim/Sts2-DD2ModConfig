using DD2ModConfig.Code.Helper;
using DD2ModConfig.Code.Relics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace DD2ModConfig.Code.Patches;

[HarmonyPatch(typeof(RunManager), "OnEnded")]
public static class OnGameEndedPatch
{
    public static void Postfix()
    {
        DD2Helper.ReparentActiveVfxNodes();
        DD2Helper.ResetAllValues();
    }
}

[HarmonyPatch(typeof(RunManager), "InitializeNewRun")]
public static class OnGameStartedPatch
{
    public static void Postfix(RunManager __instance)
    {
        RunState? state = Traverse.Create(__instance).Property("State").GetValue<RunState>();
        if (state != null)
        {
            //此时LocalContext里的NetId为空
            Player? mePlayer = state.Players.FirstOrDefault(p => p.NetId == RunManager.Instance.NetService.NetId);
            if (mePlayer != null)
            {
                TaskHelper.RunSafely(RelicCmd.Obtain(ModelDb.Relic<StressCounterRelic>().ToMutable(), mePlayer));
            }
        }
    }
}