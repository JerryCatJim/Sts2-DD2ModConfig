using DD2ModConfig.Code.Core;
using DD2ModConfig.Code.Helper;
using DD2ModConfig.Code.Hooks;
using DD2ModConfig.Code.ResoluteOrMeltdown;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace DD2ModConfig.Code.Commands;

public static class RMCmd
{
    public static async Task TryEnterResoluteOrMeltdown(PlayerChoiceContext ctx, Player player, CardModel? cardSource)
    {
        ResoluteOrMeltdownModel oldRM = RMHelper.GetResoluteOrMeltdownModel(player);
        ResoluteOrMeltdownModel newRM = RMHelper.TryGetUniqueRM(player);
        if(newRM == null || newRM == RMModelDb.ResoluteOrMeltdown<NoResoluteAndMeltdown>())
        {
            newRM = GetRandomResoluteOrMeltdown(ctx, player, cardSource);
        }
        await DD2Hooks.BeforeResoluteOrMeltdownChanged(ctx, player, oldRM, newRM);
        await EnterResoluteOrMeltdown(ctx, player, newRM, cardSource);
        await DD2Hooks.AfterResoluteOrMeltdownChanged(ctx, player, oldRM, newRM);
    }

    public static Task EnterResoluteOrMeltdown(PlayerChoiceContext ctx, Player player, ResoluteOrMeltdownModel newCanonical, CardModel? cardSource)
    {
        return RMHelper.SetResoluteOrMeltdown(ctx, player, newCanonical, cardSource);
    }
    public static Task EnterResoluteOrMeltdown<T>(PlayerChoiceContext ctx, Player player, CardModel? cardSource) where T : ResoluteOrMeltdownModel
    {
        return RMHelper.SetResoluteOrMeltdown<T>(ctx, player, cardSource);
    }

    public static Task ExitResoluteOrMeltdown(PlayerChoiceContext ctx, Player player, CardModel? cardSource)
    {
        return RMHelper.SetResoluteOrMeltdown<NoResoluteAndMeltdown>(ctx, player, cardSource);
    }
    private static ResoluteOrMeltdownModel GetRandomResoluteOrMeltdown(PlayerChoiceContext ctx, Player player, CardModel? cardSource)
    {
        //You can also use official RNG class to get syncable random values.
        int index = DD2RngHelper.GetRandomIndex(player, 100);
        //index应为[0, 99]，小于0则越界
        if (index < 0) return RMModelDb.ResoluteOrMeltdown<NoResoluteAndMeltdown>();

        //0到99,Meltdown概率80%，所以0到19为Resolute，其他为Meltdown
        bool isMeltdown = index >= DD2Hooks.ModifyResoluteprobability(player.Creature, 20m);
        if(isMeltdown)
        {
            return RMHelper.GetRandomMeltdown(player);
        }
        else
        {
            return RMHelper.GetRandomResolute(player);
        }
    }
}