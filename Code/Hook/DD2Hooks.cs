using BaseLib.Patches.Hooks;
using DD2ModConfig.Code.Abstract;
using DD2ModConfig.Code.Powers;
using DD2ModConfig.Code.ResoluteOrMeltdown;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace DD2ModConfig.Code.Hooks;

public class DD2Hooks
{
    private static async Task Dispatch<T>(PlayerChoiceContext ctx, Player player, Func<T, Task> invoke)
        where T : class
    {
        var combatState = player.Creature.CombatState;
        if (combatState == null) return;
        foreach (var model in combatState.IterateHookListeners().OfType<T>())
        {
            var abstractModel = (AbstractModel)(object)model;
            ctx.PushModel(abstractModel);
            await invoke(model);
            ctx.PopModel(abstractModel);
        }
    }

    private static TResult Aggregate<T, TResult>(CombatState combatState, TResult seed,
        Func<T, TResult, TResult> action)
        where T : class =>
        combatState.IterateHookListeners().OfType<T>()
            .Aggregate(seed, (current, model) => action(model, current));

    public static Task BeforeResoluteOrMeltdownChanged(PlayerChoiceContext ctx, Player player, ResoluteOrMeltdownModel oldRM, ResoluteOrMeltdownModel newRM)
        => Dispatch<IBeforeResoluteOrMeltdownChanged>(ctx, player, m => m.BeforeResoluteOrMeltdownChanged(ctx, player, oldRM, newRM));
    public static Task OnResoluteOrMeltdownChanged(PlayerChoiceContext ctx, Player player, ResoluteOrMeltdownModel oldRM, ResoluteOrMeltdownModel newRM)
        => Dispatch<IOnResoluteOrMeltdownChanged>(ctx, player, m => m.OnResoluteOrMeltdownChanged(ctx, player, oldRM, newRM));
    public static Task AfterResoluteOrMeltdownChanged(PlayerChoiceContext ctx, Player player, ResoluteOrMeltdownModel oldRM, ResoluteOrMeltdownModel newRM)
        => Dispatch<IAfterResoluteOrMeltdownChanged>(ctx, player, m => m.AfterResoluteOrMeltdownChanged(ctx, player, oldRM, newRM));

    public static decimal ModifyHealingHp(Creature? creature, decimal originalHealing)
    {
        if (creature == null) return originalHealing;

        /*decimal num = originalHealing;
        if (creature.CombatState != null && creature.CombatState.RunState != null)
        {
            //不用CombatState，保留在战斗外监听血量回复的可能
            foreach (AbstractModel item in creature.CombatState.RunState.IterateHookListeners(creature.CombatState))
            {
                if (item is IModifyHpAmountReceived myModel)
                {
                    myModel.TryModifyHpAmountReceived(creature, num, out var myModifiedAmount, true);
                    num = myModifiedAmount;
                }
            }
        }
        return num;*/
        return ModifyHealAmountPatches.ModifyHeal(originalHealing, creature);
    }
    
    public static decimal ModifyStressPower(ICombatState? combatState, PowerModel? stressPower, decimal deltaAmount, Creature? applier, Creature target, CardModel? cardSource)
    {
        if (combatState == null) return deltaAmount;

        if (stressPower == null)
        {
            stressPower = ModelDb.Power<StressPower>().ToMutable();
        }
        decimal modifiedAmount = stressPower.Amount + deltaAmount;
        if (applier != null && combatState.ContainsCreature(applier))
        {
            modifiedAmount = Hook.ModifyPowerAmountGiven(combatState, stressPower, applier, modifiedAmount, target, cardSource, out IEnumerable<AbstractModel>_);
        }
        modifiedAmount = Hook.ModifyPowerAmountReceived(combatState, stressPower, target, modifiedAmount, applier, out IEnumerable<AbstractModel>_);
        return modifiedAmount;
    }

    public static decimal ModifyResoluteprobability(Creature? creature, decimal originalprobability = 20m)
    {
        if (creature == null) return originalprobability;

        decimal num = originalprobability;
        if (creature.CombatState != null && creature.CombatState.RunState != null)
        {
            //不用CombatState，保留在战斗外监听的可能
            foreach (AbstractModel item in creature.CombatState.RunState.IterateHookListeners(creature.CombatState))
            {
                if (item is IModifyResoluteprobability myModel)
                {
                    myModel.TryModifyResoluteprobability(creature, num, out var myModifiedAmount);
                    num = myModifiedAmount;
                }
            }
        }
        return num;
    }
}