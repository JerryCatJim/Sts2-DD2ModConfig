using DD2ModConfig.Code.Abstract;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace DD2ModConfig.Code.Powers;

public class DefaultMeltdownPower : DD2ModConfigPowerModel
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("HpSetPercent", 10m),
        new DynamicVar("DeathArmorAmount", 1m)
    ];

    public void SetHpSetPercent(decimal hpSetPercent, decimal deathArmorAmount)
    {
        AssertMutable();
        base.DynamicVars["HpSetPercent"].BaseValue = 10m;
        base.DynamicVars["DeathArmorAmount"].BaseValue = deathArmorAmount;
    }
}
