using DD2ModConfig.Code.Compatibility;
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
        ResoluteOrMeltdownModel newRM = DD2Helper.TryGetUniqueRM(player);
        if(newRM == null || newRM == RMModelDb.ResoluteOrMeltdown<NoResoluteAndMeltdown>())
        {
            newRM = GetRandomResoluteOrMeltdown(ctx, player, cardSource);
        }
        await DD2Hooks.BeforeResoluteOrMeltdownChanged(ctx, player, oldRM, newRM);
        await RMHelper.SetResoluteOrMeltdown(ctx, player, newRM, cardSource);
        await DD2Hooks.AfterResoluteOrMeltdownChanged(ctx, player, oldRM, newRM);
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
        int playerMaxHP = player.Creature?.MaxHp ?? 0;
        int floorCount  = player.RunState?.TotalFloor ?? 0;
        int roundNumber = player.Creature?.CombatState?.RoundNumber ?? 0;

        //int index0 = player.PlayerRng.Transformations.NextInt(0, 9);
        int index1 = GetRandomIndex(player, playerMaxHP, floorCount, roundNumber, 100);

        //0到99,Meltdown概率80%，所以0到19为Resolute，其他为Meltdown
        bool isMeltdown = index1 >= DD2Hooks.ModifyResoluteprobability(player.Creature, 20m);
        if(isMeltdown)
        {
            IReadOnlyList<RMRegistration> MeltdownList = RMRegistry.Query(r => r.RMType == ResoluteOrMeltdownType.Meltdown);
            int index2 = GetRandomIndex(player, playerMaxHP, floorCount, roundNumber, MeltdownList.Count);

            if (index2 <= 0) return RMModelDb.ResoluteOrMeltdown<NoResoluteAndMeltdown>();

            return MeltdownList[index2].Factory();
        }
        else
        {
            IReadOnlyList<RMRegistration> ResoluteList = RMRegistry.Query(r => r.RMType == ResoluteOrMeltdownType.Resolute);
            int index3 = GetRandomIndex(player, playerMaxHP, floorCount, roundNumber, ResoluteList.Count);

            if (index3 <= 0) return RMModelDb.ResoluteOrMeltdown<NoResoluteAndMeltdown>();

            return ResoluteList[index3].Factory();
        }
    }
    public static int GetRandomIndex(Player player, int a, int b, int c, int N)
    {
        if (N <= 0)
        {
            return -1;
        }

        ulong seed = DD2SeedCompat.GetPlayerRngSetSeed(player.PlayerRng);     //盐值，增加分散度

        seed = (seed ^ (ulong)a) * 0x9E3779B9u; // 混入 a
        seed = (seed ^ (ulong)b) * 0x85EBCA6Bu; // 混入 b
        seed = (seed ^ (ulong)c) * 0x7A3CFD3Bu; // 混入 c

        // 额外扩散：让高位和低位互相影响
        seed ^= (seed >> 16);
        seed *= 0x85EBCA6Bu;
        seed ^= (seed >> 13);
        seed *= 0x7A3CFD3Bu;
        seed ^= (seed >> 16);

        return (int)(seed % (ulong)N);
    }
}