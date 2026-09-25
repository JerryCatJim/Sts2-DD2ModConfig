using DD2ModConfig.Code.ResoluteOrMeltdown.Vfx;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace DD2ModConfig.Code.ResoluteOrMeltdown;

public class NoResoluteAndMeltdown : ResoluteOrMeltdownModel
{
    public override bool ShouldReceiveCombatHooks => false;
    public override IEnumerable<PowerModel>? DisplayPowers => null;
    public override ResoluteOrMeltdownType RMType => ResoluteOrMeltdownType.None;
    protected override VfxConfig RMVfxConfig => new(
        EnterSfxPath: "",
        ScreenShakeStrength: ShakeStrength.None
    );
}