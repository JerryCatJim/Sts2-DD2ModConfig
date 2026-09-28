using BaseLib.Config;

namespace DD2ModConfig.Code.Config;

[ConfigHoverTipsByDefault]
internal class DD2ModConfigCfg : SimpleModConfig
{
    // Should likely be at the top, as an easy and obvious opt-out

    [ConfigSection("DD2Config")]
    public static bool ShouldPlayDeathDoorVfxIfPoisoned { get; set; } = true;
    public static bool ShouldPlayDeathDoorVfxIfDoomed { get; set; } = true;
    public static bool ShouldPlayDeathDoorVfxIfLowHealth { get; set; } = true;
    [ConfigSlider(0, 100, 1, Format = "{0:0} %")]
    public static int PlayerShowDeathDoorVfxHpPercent { get; set; } = 25;
    [ConfigSlider(0, 100, 1, Format = "{0:0} %")]
    public static int MonsterShowDeathDoorVfxHpPercent { get; set; } = 10;
    public static bool ShouldPlayerShowDeathDoorVfx { get; set; } = true;
    public static bool ShouldMonsterShowDeathDoorVfx { get; set; } = true;
    public static bool ShouldPlayerShowDeathArmorVfx { get; set; } = true;
    public static bool ShouldMonsterShowDeathArmorVfx { get; set; } = true;
    public static bool ShouldPlayerShowDeathBlowVfx { get; set; } = true;
    public static bool ShouldMonsterShowDeathBlowVfx { get; set; } = true;

    [ConfigSection("DeathVfxAndBgm")]
    public static bool ShouldShowDeathEncounterVfx { get; set; } = true;
    public static bool ShouldPlayDeathEncounterBgm { get; set; } = true;
    [ConfigSection("DeathEncounter")]
    public static bool ShouldMultiplayerUseDefaultCondition { get; set; } = true;
    public static bool ShouldDeathOnlyHuntFlagellant { get; set; } = true;

    [ConfigSlider(0, 100, 1, Format = "{0:0}%")]
    public static int DeathEncounterChance { get; set; } = 6;

    [ConfigSlider(1, 99, 1)]
    public static int DeathAppearMaxTime { get; set; } = 1;
    public static bool ShouldEnhanceDeathAfterDefeat { get; set; } = true;
    public static bool PredictWhetherDeathWillAppear { get; set; } = true;
    public static bool ShouldDeathAppearInMonsterRoom { get; set; } = true;
    public static bool ShouldDeathAppearInEliteRoom { get; set; } = false;
    public static bool ShouldDeathAppearInBossRoom { get; set; } = false;
}