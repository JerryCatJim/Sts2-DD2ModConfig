using DD2ModConfig.Code.Attributes;
using DD2ModConfig.Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace DD2ModConfig.Code.ResoluteOrMeltdown.Resolute;

[RMRegister(ResoluteOrMeltdownType.Meltdown)]
public class DefaultMeltdown : ResoluteOrMeltdownModel
{
    private decimal _setHpPercent = 10m;
    private decimal _deathArmorAmount = 1m;
    public override IEnumerable<PowerModel> DisplayPowers =>
    [
        ModelDb.Power<DefaultMeltdownPower>().ToMutable()
    ];

    public override ResoluteOrMeltdownType RMType => ResoluteOrMeltdownType.Meltdown;

    public override async Task OnEnterResoluteOrMeltdown(PlayerChoiceContext choiceContext, Player player, CardModel? source)
    {
        if (player == null || player.Creature == null) return;

        (await PowerCmd.Apply<DefaultMeltdownPower>(choiceContext, player.Creature, 1, null, source, silent: true))?.SetHpSetPercent(_setHpPercent, _deathArmorAmount);
        await PowerCmd.Apply<DeathArmorPower>(choiceContext, player.Creature, _deathArmorAmount, null, source, silent: true);
        await CreatureCmd.SetCurrentHp(player.Creature, Math.Round(player.Creature.MaxHp * _setHpPercent / 100m, MidpointRounding.AwayFromZero));
    }

    public override async Task OnExitResoluteOrMeltdown(PlayerChoiceContext choiceContext, Player player, CardModel? source)
    {
        if (player == null || player.Creature == null) return;

        await PowerCmd.Remove<DefaultMeltdownPower>(player.Creature);

        await base.OnEnterResoluteOrMeltdown(choiceContext, player, source);
    }
}
