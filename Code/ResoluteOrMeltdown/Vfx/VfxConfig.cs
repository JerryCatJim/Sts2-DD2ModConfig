using Godot;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace DD2ModConfig.Code.ResoluteOrMeltdown.Vfx;

public record VfxConfig(
    string? EnterSfxPath = null,
    //Color? ScreenFlashColor = null,
    ShakeStrength ScreenShakeStrength = ShakeStrength.None
)
{

}