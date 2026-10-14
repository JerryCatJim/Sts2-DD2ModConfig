using DD2ModConfig.Code.Attributes;
using DD2ModConfig.Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace DD2ModConfig.Code.ResoluteOrMeltdown.Resolute;

[RMRegister(ResoluteOrMeltdownType.Resolute)]
public class DefaultResolute : ResoluteOrMeltdownModel
{
    private decimal _healHpPercent = 50m;
    public override IEnumerable<PowerModel> DisplayPowers =>
    [
        ModelDb.Power<DefaultResolutePower>().ToMutable()
    ];

    public override ResoluteOrMeltdownType RMType => ResoluteOrMeltdownType.Resolute;

    public override async Task OnEnterResoluteOrMeltdown(PlayerChoiceContext choiceContext, Player player, CardModel? source)
    {
        if (player == null || player.Creature == null) return;

        (await PowerCmd.Apply<DefaultResolutePower>(choiceContext, player.Creature, 1, null, source, silent : true))?.SetHealPercent(_healHpPercent);
        await CreatureCmd.Heal(player.Creature, Math.Round(player.Creature.MaxHp * _healHpPercent / 100m, MidpointRounding.AwayFromZero));
    }

    public override async Task OnExitResoluteOrMeltdown(PlayerChoiceContext choiceContext, Player player, CardModel? source)
    {
        if (player == null || player.Creature == null) return;

        await PowerCmd.Remove<DefaultResolutePower>(player.Creature);

        await base.OnExitResoluteOrMeltdown(choiceContext, player, source);
    }
}
