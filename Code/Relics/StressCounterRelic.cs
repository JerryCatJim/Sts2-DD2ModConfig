using BaseLib.Utils;
using DD2ModConfig.Code.Abstract;
using DD2ModConfig.Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace DD2ModConfig.Code.Relics;

[Pool(typeof(DarkestDungeonRelicPool))]
public class StressCounterRelic : DarkestDungeonRelicModel, IAfterStressChanged
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => true;

    public override int DisplayAmount
    {
        get
        {
            //return base.DynamicVars["StressCount"].IntValue;
            return CurrentStress;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        //new DynamicVar("StressCount", 0m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StressPower>()
    ];

    private bool _shouldChangeStress = true;
    private int _currentStress = 0;
    [SavedProperty]
    public int CurrentStress
    {
        get
        {
            return _currentStress;
        }
        set
        {
            if (_shouldChangeStress)
            {
                AssertMutable();
                _currentStress = value;
                InvokeDisplayAmountChanged();
            }
        }
    }

    public Task AfterStressAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (amount != 0 && power != null && power.Owner.Player == Owner)
        {
            CurrentStress += (int)amount;
        }
        return Task.CompletedTask;
    }
    public override Task AfterCombatEnd(CombatRoom room)
    {
        CurrentStress = Owner.Creature.GetPower<StressPower>()?.Amount ?? CurrentStress;
        return Task.CompletedTask;
    }

    //记录上一场战斗结束时的压力量，在下场战斗开始应用回来，达成"战斗外保留压力"的效果
    public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == null) return;
        if (player == Owner && player.PlayerCombatState?.TurnNumber == 1)
        {
            if (CurrentStress > 0)
            {
                _shouldChangeStress = false;
                await PowerCmd.Apply<StressPower>(choiceContext, player.Creature, CurrentStress, player.Creature, null);//, true);
                _shouldChangeStress = true;
            }
        }
    }
}