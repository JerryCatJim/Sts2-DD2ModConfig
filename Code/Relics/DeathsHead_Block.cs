using BaseLib.Utils;
using DD2ModConfig.Code.Abstract;
using DD2ModConfig.Code.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace DD2ModConfig.Code.Relics;

[Pool(typeof(DarkestDungeonRelicPool))]
public class DeathsHead_Block : DarkestDungeonRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("LowHealthPercent", 25m),
        new BlockVar(13m, ValueProp.Unpowered)
    ];

    protected override string BigIconPath
    {
        get
        {
            return "deaths_head.png".RelicImagePath(CurrentModId);
        }
    }
    public override string PackedIconPath
    {
        get
        {
            return BigIconPath;
        }
    }
    protected override string PackedIconOutlinePath
    {
        get
        {
            return "deaths_head_outline.png".RelicImagePath(CurrentModId);
        }
    }

    public override async Task AfterBlockCleared(Creature creature) //抄的HornCleat
    {
        if (creature == base.Owner.Creature && IsLowHealth())// && base.Owner.PlayerCombatState?.TurnNumber == 2)
        {
            Flash();
            await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, null);
        }
    }

    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature == base.Owner.Creature)
        {
            base.Status = IsLowHealth() ? RelicStatus.Active : RelicStatus.Normal;
        }
        return Task.CompletedTask;
    }

    public override Task AfterObtained()
    {
        base.Status = IsLowHealth() ? RelicStatus.Active : RelicStatus.Normal;
        return Task.CompletedTask;
    }

    public override Task BeforeCombatStart()
    {
        base.Status = IsLowHealth() ? RelicStatus.Active : RelicStatus.Normal;
        return Task.CompletedTask;
    }

    public override Task AfterCombatVictory(CombatRoom room)
    {
        base.Status = RelicStatus.Normal;
        return Task.CompletedTask;
    }

    private bool IsLowHealth()
    {
        if (base.Owner.Creature == null) return false;

        decimal Percent = DynamicVars.ContainsKey("LowHealthPercent") ? DynamicVars["LowHealthPercent"].BaseValue : 0m;
        return (decimal)base.Owner.Creature.CurrentHp / (decimal)base.Owner.Creature.MaxHp * 100m <= Percent;
    }
}
