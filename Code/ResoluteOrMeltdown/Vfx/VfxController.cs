using DD2ModConfig.Code.Audio;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace DD2ModConfig.Code.ResoluteOrMeltdown.Vfx;

public class VfxController(VfxConfig cfg)
{
    public async Task OnEnter(Creature owner)
    {
        PlayEnterSfx();
        if (LocalContext.IsMe(owner))
        {
            //PlayScreenFlash();
            PlayScreenShake();
        }
        await Task.CompletedTask;
    }

    public async Task OnExit(Creature owner)
    {
        await Task.CompletedTask;
    }

    // ── SFX ───────────────────────────────────────

    private void PlayEnterSfx()
    {
        if (!string.IsNullOrEmpty(cfg.EnterSfxPath))
            RMAudioManager.PlayRMSfx(cfg.EnterSfxPath, bIsTemp : false, bIsFullPathName : false, -4);
    }

    /*private void PlayScreenFlash()
    {
        if (cfg.ScreenFlashColor != null)
            ScreenFlashEffect.Play(cfg.ScreenFlashColor.Value);
    }*/

    private void PlayScreenShake()
    {
        if (cfg.ScreenShakeStrength != ShakeStrength.None)
            NGame.Instance?.ScreenShake(cfg.ScreenShakeStrength, ShakeDuration.Short);
    }
}