using BaseLib.Utils;
using DD2ModConfig.Code.Abstract;
using DD2ModConfig.Code.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
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

    private bool _initialized = false;
    //因意外状况，战斗开始时，在压力计数器初始化角色压力之前，就已经给予角色的压力累计值
    private decimal _preAppliedStressNum = 0m;

    public Task AfterStressAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (amount != 0 && power != null && power.Owner.Player == Owner)
        {
            CurrentStress += (int)amount;
            if (!_initialized)  //仅战斗内才会监听到变化
            {
                _preAppliedStressNum += amount;
            }
        }
        return Task.CompletedTask;
    }

    public override Task BeforeCombatStart()
    {
        ResetValue();
        return Task.CompletedTask;
    }
    public override Task AfterCombatEnd(CombatRoom room)  //先于AfterCombatVictory
    {
        CurrentStress = Owner.Creature.GetPower<StressPower>()?.Amount ?? CurrentStress;
        ResetValue();
        return Task.CompletedTask;
    }

    //记录上一场战斗结束时的压力量，在下场战斗开始应用回来，达成"战斗外保留压力"的效果
    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (participants.Contains(Owner.Creature) && Owner.PlayerCombatState?.TurnNumber == 1)
        {
            if (CurrentStress > 0)
            {
                //从这里施加的压力又会触发AfterStressAmountChanged导致计数重复，所以排除掉
                _shouldChangeStress = false;
                await PowerCmd.Apply<StressPower>(choiceContext, Owner.Creature, (decimal)CurrentStress - _preAppliedStressNum, Owner.Creature, null);//, silent : true);
                _shouldChangeStress = true;

                _initialized = true;
                _preAppliedStressNum = 0m;
            }
        }
    }
    private void ResetValue()
    {
        _shouldChangeStress = true;
        _initialized = false;
        _preAppliedStressNum = 0m;
    }
}