using DD2ModConfig.Code.Abstract;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace DD2ModConfig.Code.Helper;

public static class PoisonPowerHelper
{
    private static int GetTriggerCount(PoisonPower poisonPower, int powerAmount)
    {
        if (poisonPower == null || powerAmount <= 0 || poisonPower.Owner == null ||poisonPower.Owner.CombatState == null) return 0;
        IEnumerable<Creature> source = from c in poisonPower.Owner.CombatState.GetOpponentsOf(poisonPower.Owner)
                                       where c.IsAlive
                                       select c;
        return Math.Min(powerAmount, 1 + source.Sum((Creature a) => a.GetPowerAmount<AccelerantPower>()));
    }

    public static int CalculateTotalDamageNextTurn(PoisonPower poisonPower, int powerAmount)
    {
        if (poisonPower == null || powerAmount <= 0 || poisonPower.Owner == null || poisonPower.Owner.CombatState == null) return 0;

        decimal num = default(decimal);
        int num2 = Math.Min(powerAmount, GetTriggerCount(poisonPower, powerAmount));
        for (int i = 0; i < num2; i++)
        {
            decimal damage = powerAmount - i;
            damage = Hook.ModifyDamage(poisonPower.Owner.CombatState.RunState, poisonPower.Owner.CombatState, poisonPower.Owner, null, damage, ValueProp.Unblockable | ValueProp.Unpowered, null, null, ModifyDamageHookType.All, CardPreviewMode.None, out IEnumerable<AbstractModel> _);
            num += damage;
        }
        return (int)num;
    }
    public static int CalculateTotalDamageByCount(PoisonPower poisonPower, int count)
    {
        if (poisonPower == null || count <= 0 || poisonPower.Owner == null || poisonPower.Owner.CombatState == null) return 0;

        decimal num = default(decimal);
        int num2 = Math.Min(poisonPower.Amount, count);
        for (int i = 0; i < num2; i++)
        {
            decimal damage = poisonPower.Amount - i;
            damage = Hook.ModifyDamage(poisonPower.Owner.CombatState.RunState, poisonPower.Owner.CombatState, poisonPower.Owner, null, damage, ValueProp.Unblockable | ValueProp.Unpowered, null, null, ModifyDamageHookType.All, CardPreviewMode.None, out IEnumerable<AbstractModel> _);
            num += damage;
        }
        return (int)num;
    }

    public static void BroadcastPoisonBeforeTriggered(ICombatState combatState, PowerModel power, decimal amount)
    {
        if (combatState != null)
        {
            foreach (AbstractModel item in combatState.IterateHookListeners())
            {
                if (item is IBeforePoisonTrigger myModel)
                {
                    myModel.BeforePoisonTrigger(power, amount);
                }
            }
        }
    }
}

[HarmonyPatch(typeof(PoisonPower), nameof(PoisonPower.AfterSideTurnStart))]
public static class PoisonPowerBeforeTriggeredPatch
{
    public static bool Prefix(PoisonPower __instance, IReadOnlyList<Creature> participants)
    {
        if (!participants.Contains(__instance.Owner))
        {
            return true;
        }
        ICombatState? combatState = __instance.Owner.CombatState;
        if (combatState != null)
        {
            PoisonPowerHelper.BroadcastPoisonBeforeTriggered(combatState, __instance, __instance.Amount);
        }
        return true;
    }
}