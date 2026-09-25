using DD2ModConfig.Code.Abstract;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace DD2ModConfig.Code.Powers;

public sealed class DeathArmorPower : DD2ModConfigPowerModel
{
    public override PowerType Type => PowerType.None;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        
    ];

    public override bool ShouldDie(Creature creature)
    {
        if (creature != base.Owner)
        {
            return true;
        }
        return Amount <= 0;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        await CreatureCmd.SetCurrentHp(creature, 1m);
        await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), this, -1, Owner, null, true);
    }
}
