using DD2ModConfig.Code.Abstract;
using DD2ModConfig.Code.Commands;
using DD2ModConfig.Code.Core;
using DD2ModConfig.Code.ResoluteOrMeltdown;
using DD2ModConfig.Code.Singleton;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace DD2ModConfig.Code.Powers;

public sealed class StressPower : DD2ModConfigPowerModel
{
    public override bool AllowNegative => true; //Creature.cs里的InvokePowerModified()要求 AllowNegative==true才能把层数减少的事件传过来
    public override PowerType Type => PowerType.None;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            var characterId = Owner.Player?.Character.Id.Entry;
            if (string.IsNullOrEmpty(characterId)) yield break;

            var entry = RMRegistry.GetUniqueList(characterId).FirstOrDefault();
            if (entry == null) yield break;

            var rmModel = entry.Factory();
            if (rmModel == null) yield break;

            yield return RMHoverTipFactory.FromResoluteOrMeltdown(rmModel);

            if (rmModel.DisplayPowers == null || rmModel.DisplayPowers.Count() == 0) yield break;
            foreach (var powerModel in rmModel.DisplayPowers)
            {
                if (powerModel == null) continue;
                yield return HoverTipFactory.FromPower(powerModel);
            }
        }
    }

    private bool WaitingForRemoving = false;
    private bool HasLocked => Owner.Player != null && 
        DD2CombatSingleton.StressLockDictionary.TryGetValue(Owner.Player.NetId, out int OutValue) && OutValue > 0;
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (amount == 0m || power is not StressPower || power != this)
            return;

        if (HasLocked)
        {
            Log.Error("[Stress Lock Error] : Stress amount change INFINITE LOOP detected! Check your design to avoid it!");
            return;
        }

        TrySetStressLock(1);
        try
        {
            //有效的变化层数，例如Amount当前为-2，amount为-3，即修改之前Amount为1，减少了1层，所以传入的是-1
            decimal realChangedAmount = Amount < 0 ? amount - Amount : amount;
            if (realChangedAmount != 0m)
            {
                await BroadcastStressChangedEvent(choiceContext, power, realChangedAmount, applier, cardSource);
            }

            //满10点压力触发美德或者折磨判定，触发后把压力值归零
            if (Amount >= 10)
            {
                await BroadcastStressChangedEvent(choiceContext, power, -Amount, applier, cardSource);
                await SetAmountBorder();
                if (Owner.Player is Player player)
                {
                    await RMCmd.TryEnterResoluteOrMeltdown(choiceContext, player, cardSource);
                }
            }
            await SetAmountBorder();
        }
        finally
        {
            TrySetStressLock(0);
        }
    }
    
    private async Task BroadcastStressChangedEvent(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (amount == 0m || base.Owner.CombatState == null) return;

        foreach (AbstractModel item in base.Owner.CombatState.IterateHookListeners())
        {
            if (item is IAfterStressChanged myModel)
            {
                await myModel.AfterStressAmountChanged(choiceContext, power, amount, applier, cardSource);
            }
        }
    }
    private async Task SetAmountBorder()
    {
        if ((Amount < 0 || Amount >= 10) && !WaitingForRemoving)
        {
            WaitingForRemoving = true;
            await PowerCmd.Remove(this);
        }
    }

    private void TrySetStressLock(int Value)
    {
        if (Owner.Player == null) return;

        DD2CombatSingleton.StressLockDictionary[Owner.Player.NetId] = Value;
    }
}
