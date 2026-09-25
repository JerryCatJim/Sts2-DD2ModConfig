/*using DD2ModConfig.Code.Hooks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace DD2ModConfig.Code.Patches;

[HarmonyPatch(typeof(CreatureCmd), "Heal")]
public static class CreatureCmdHealPatch
{
    public static bool Prefix(Creature creature, ref decimal amount)
    {
        if (CombatManager.Instance.IsEnding && !creature.IsPlayer)
        {
            return true;
        }
        amount = DD2Hooks.ModifyHealingHp(creature, amount);
        return true;
    }
}*/