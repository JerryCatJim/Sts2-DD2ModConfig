using DD2ModConfig.Code.Audio;
using DD2ModConfig.Code.Config;
using DD2ModConfig.Code.Helper;
using DD2ModConfig.Code.Monster;
using DD2ModConfig.Code.Monster.DeathMonster;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

namespace DD2ModConfig.Code.Patches;

[HarmonyPatch(typeof(NCreature), "SetAnimationTrigger")]
public static class DeathAnimPatch
{
    public static void Postfix(NCreature __instance, string trigger)
    {
        if (__instance.Entity == null || __instance.Entity.Monster is not Death)
            return;

        if (__instance.Entity.ModelId.ToString() == "MONSTER.DD2MODCONFIG-DEATH")
        {
            Log.Info("[>>>DD2ModConfig Monster AnimeTrigger=]" + trigger);
        }

        switch (trigger)
        {
            case "Hit":
                PlayAnim(__instance, "Hit", false);
                break;

            case "":
            case null:
                break;

            case "Dead":
                PlayAnim(__instance, "Dead", true);
                break;

            case "Revive":
            case "Idle":
                PlayAnim(__instance, "Idle");
                break;

            default:
                PlayAnim(__instance, trigger, false, true);
                break;
        }
    }

    private static void PlayAnim(NCreature node, string animName, bool playImmediately = false, bool bHasChildStateMachine = false)
    {
        var visual = node.Visuals;
        if (visual == null) return;

        var animPlayer = visual.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        if (animPlayer == null) return;

        var animTree = visual.GetNodeOrNull<AnimationTree>("AnimationTree");
        if (animTree == null) return;

        DD2MonsterHelper.ResetMonsterAdvancedConditions(animTree, node.Entity);

        var state_machine = (AnimationNodeStateMachinePlayback)animTree.Get("parameters/playback");

        if (state_machine != null)
        {
            if (animName == "Hit")
            {
                if (state_machine.GetCurrentNode() == "Spawn" || state_machine.GetCurrentNode() == "Hit" || state_machine.GetCurrentNode() == "DeathDoorHit")
                { 
                    return;
                }
                MonsterAudioManager.PlayDeathSfx("Hit",true);
                animName = DD2Helper.IsInDeathDoor(node.Entity) || node.Entity.CurrentHp == 1m ? "DeathDoorHit" : "Hit";
            }
            if (!bHasChildStateMachine)
            {
                if (playImmediately)
                {
                    state_machine.Start(animName);
                }
                else
                {
                    state_machine.Travel(animName);
                }
            }
            if (bHasChildStateMachine)
            {
                if (animName.Contains("Attack/"))
                {
                    var Attack_SM = (AnimationNodeStateMachinePlayback)animTree.Get("parameters/Attack/playback");

                    if (state_machine != null && Attack_SM != null)
                    {
                        string attackAnimName = animName.Replace("Attack/", "");
                        if (!string.IsNullOrEmpty(attackAnimName))
                        {
                            if (!Attack_SM.IsConnected("state_started", _stateStartedCallable))
                            {
                                //持续监听,不要OnShot
                                Attack_SM.Connect("state_started", _stateStartedCallable);
                            }
                            state_machine.Start("Attack");
                            Attack_SM.Travel(attackAnimName);
                        }
                    }
                }
            }
        }
    }
    private static readonly Callable _stateStartedCallable = Callable.From((StringName state) =>
    {
        bool isTemp = state.ToString().Contains("Recover");// || state.ToString().Contains("Action");
        MonsterAudioManager.PlayDeathSfx(state, isTemp);
    });
}

[HarmonyPatch(typeof(NBgmVolumeSlider), "OnValueChanged")]
public static class DeathBgmPatch
{
    public static void Postfix(double value)
    {
        //从死神的战斗中退回到主界面会导致BGM音量为0，调一下滑动条就恢复正常了，没找到退回到主界面事件，懒得修了
        if (CombatManager.Instance.IsInProgress && DeathListenForRunStateSingleton.IsDeathExistingInCombat && DD2ModConfigCfg.ShouldPlayDeathEncounterBgm)
        {
            NAudioManager.Instance?.SetBgmVol(0);
            DD2AudioManager.SetDD2BgmPlayerVolumeByPercent((float)(value / 100.0));
        }
    }
}