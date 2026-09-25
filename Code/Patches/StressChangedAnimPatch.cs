using DD2ModConfig.Code.Audio;
using DD2ModConfig.Code.Powers;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace DD2ModConfig.Code.Patches;

[HarmonyPatch(typeof(NCreature), "OnPowerIncreased")]
public static class StressIncreaseAnimPatch
{
    //需要把继承自PowerModel的Power里的AllowNegative改为true才能接收到amount小于0的情况，详见Creature.cs里的InvokePowerModified()
    public static bool Prefix(NCreature __instance, PowerModel power, int amount, bool silent)
    {
        if (silent || !CombatManager.Instance.IsInProgress) return true;

        //if (__instance.Entity.IsPlayer == false) return true;
        if (amount == 0) return true;

        if (power is StressPower)// || power is ResoluteOrMeltdownPowerModel)
        {
            if (power is StressPower)
            {
                string NodeName = amount > 0 ? "StressUp" : "StressDown";
                Node2D StressNode = __instance.Visuals.GetNodeOrNull<Node2D>(NodeName + "Node");
                if (StressNode == null)
                {
                    StressNode = PreloadManager.Cache.GetScene("res://DD2ModConfig/Scenes/DD2Scenes/" + NodeName + ".tscn").Instantiate<Node2D>();
                }
                if (StressNode == null)
                {
                    Log.Error("[StressIncreaseAnimPatch]: " + NodeName + ".tscn load failed.");
                    return true;
                }
                else
                {
                    if (__instance != null && StressNode.GetParent() == null)
                    {
                        Marker2D? stressPosMarker = __instance.Visuals.GetNodeOrNull<Marker2D>("%StressPos");
                        if (stressPosMarker != null)
                        {
                            stressPosMarker.AddChild(StressNode);
                            StressNode.Position = new Vector2(0, 0);
                        }
                        else
                        {
                            __instance.AddChild(StressNode);
                            StressNode.Position = new Godot.Vector2(0, __instance.Visuals.GetNodeOrNull<Control>("%Bounds")?.Position.Y ?? 0);
                            StressNode.Position *= __instance.Visuals.Scale;
                            StressNode.Scale *= __instance.Visuals.Scale;
                        }
                    }
                }

                var AnimPlayer = StressNode.GetNodeOrNull<AnimationPlayer>(NodeName + "AnimPlayer");
                if (AnimPlayer != null)
                {
                    AnimPlayer.Stop();
                    AnimPlayer.Play("Show");
                    AnimPlayer.AnimationFinished += (animName) =>
                    {
                        //每次都是新生成的压力动画结点，不用判断是否重复连接信号
                        if (StressNode != null && !StressNode.IsQueuedForDeletion())
                        {
                            StressNode.QueueFreeSafely();
                        }
                    };
                }
                var StressValueText = StressNode.GetNodeOrNull<Label>(NodeName + "Text");
                if (StressValueText != null)
                {
                    StressValueText.SetText(Math.Abs(amount).ToString());
                }
                if (amount > 0)
                {
                    DD2AudioManager.PlayDD2Sfx("GainStress", false, false, -4);
                }
                else
                {
                    DD2AudioManager.PlayDD2Sfx("LoseStress", false, false, -6);
                }
            }
            else if (power is ResoluteOrMeltdownPowerModel)
            {

            }
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(NCreature), "OnPowerRemoved")]
public static class StressRemovedAnimPatch
{
    public static bool Prefix(NCreature __instance, PowerModel power)
    {
        if (power is StressPower)// || power is ResoluteOrMeltdownPowerModel)
        {
            return false;
        }
        return true;
    }
}