using BaseLib.Utils;
using DD2ModConfig.Code.Abstract;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace DD2ModConfig.Code.Relics;

[Pool(typeof(DarkestDungeonRelicPool))]
public class DeathsHead : DarkestDungeonRelicModel
{
    private bool _wasUsed;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool IsUsedUp => _wasUsed;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new HealVar(100m)];

    [SavedProperty]
    public bool WasUsed
    {
        get
        {
            return _wasUsed;
        }
        set
        {
            AssertMutable();
            _wasUsed = value;
            if (IsUsedUp)
            {
                base.Status = RelicStatus.Disabled;
            }
        }
    }

    public override bool ShouldDieLate(Creature creature)
    {
        if (creature != base.Owner.Creature)
        {
            return true;
        }
        if (WasUsed)
        {
            return true;
        }
        return false;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        Flash();
        WasUsed = true;
        decimal amount = Math.Max(1m, Math.Round(creature.MaxHp * (base.DynamicVars.Heal.BaseValue / 100m), MidpointRounding.AwayFromZero));
        await CreatureCmd.Heal(creature, amount);
    }
}