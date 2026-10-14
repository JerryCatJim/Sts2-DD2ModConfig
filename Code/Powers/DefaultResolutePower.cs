using DD2ModConfig.Code.Abstract;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace DD2ModConfig.Code.Powers;

public class DefaultResolutePower : DD2ModConfigPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("HealPercent", 50m)
    ];

    public void SetHealPercent(decimal healPercent)
    {
        AssertMutable();
        base.DynamicVars["HealPercent"].BaseValue = healPercent;
    }
}
