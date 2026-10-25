using BaseLib.Abstracts;
using DD2ModConfig.Code.Helper;
using DD2ModConfig.Code.Monster.DeathMonster;
using DD2ModConfig.Code.NetMessage;
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
                if (DD2Helper.IsHostPlayer(mePlayer))
                {
                    DeathListenForRunStateSingleton.RefreshValueByLocalConfig();
                    var msg = new SyncSpawnDeathMessage();
                    CustomMessageWrapper.Send(msg);
                }
                TaskHelper.RunSafely(RelicCmd.Obtain(ModelDb.Relic<StressCounterRelic>().ToMutable(), mePlayer));
            }
        }
    }
}

[HarmonyPatch(typeof(RunManager), "InitializeSavedRun")] 
public static class OnGameStartedFromSavePatch
{
    public static void Postfix(RunManager __instance)
    {
        RunState? state = Traverse.Create(__instance).Property("State").GetValue<RunState>();
        if (state != null)
        {
            //直接复制的上面的，不知道此时LocalContext里的NetId是否为空，反正不会出错
            Player? mePlayer = state.Players.FirstOrDefault(p => p.NetId == RunManager.Instance.NetService.NetId);
            if (mePlayer != null)
            {
                if (DD2Helper.IsHostPlayer(mePlayer))
                {
                    DeathListenForRunStateSingleton.RefreshValueByLocalConfig();
                    var msg = new SyncSpawnDeathMessage();
                    CustomMessageWrapper.Send(msg);
                }
            }
        }
    }
}