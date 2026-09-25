using DD2ModConfig.Code.Abstract;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace DD2ModConfig.Code.Powers;

public sealed class ComboPower : DD2ModConfigPowerModel
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override bool AllowNegative => true;

    public Creature? LastApplier { get; set; } = null;

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        //PowerCmd.Remove不会触发ModifyAmount，所以不会走进这里
        if (amount == 0m || power is not ComboPower || power != this) return;

        //Decrement可以触发ModifyAmount但是applier为null，所以触发ComboPower要直接用PowerCmd.ModifyAmount并传入applier
        if (applier != null)
        {
            //有效的变化层数，例如Amount当前为-2，amount为-3，即修改之前Amount为1，减少了1层，所以传入的是-1
            decimal realChangedAmount = Amount < 0 ? amount - Amount : amount;
            if(realChangedAmount != 0)
            {
                await BroadcastComboChangedEvent(choiceContext, power, amount, applier, cardSource);
                if (realChangedAmount > 0)
                {
                    LastApplier = applier;
                }
            }
        }
        await SetAmountBorder();
    }
    private async Task BroadcastComboChangedEvent(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature applier, CardModel? cardSource)
    {
        if (amount == 0m || applier == null || base.Owner.CombatState == null) return;
        
        foreach (AbstractModel item in base.Owner.CombatState.IterateHookListeners())
        {
            if (item is IAfterComboChanged myModel)
            {
                await myModel.AfterComboChanged(choiceContext, power, amount, applier, cardSource);
            }
        }
    }

    private async Task SetAmountBorder()
    {
        if (Amount > 1)
        {
            SetAmount(1, true);
        }
        else if(Amount < 0)
        {
            await PowerCmd.Remove(this);
        }
    }
}