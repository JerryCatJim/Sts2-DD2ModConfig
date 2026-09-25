using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using DD2ModConfig.Code.ResoluteOrMeltdown;
using DD2ModConfig.Code.Hooks;
using DD2ModConfig.Code.Core;
using MegaCrit.Sts2.Core.Combat;

namespace DD2ModConfig.Code.Helper;

public static class RMHelper
{
    private static readonly SpireField<Player, ResoluteOrMeltdownModel> ActiveRM =
        new(RMModelDb.ResoluteOrMeltdown<NoResoluteAndMeltdown>);
    
    public static bool InitActiveRM(CombatState? State)
    {
        if (State == null) return false;
        foreach (var player in State.Players)
        {
            ActiveRM[player] = RMModelDb.ResoluteOrMeltdown<NoResoluteAndMeltdown>();
        }
        return true;
    }
    public static ResoluteOrMeltdownModel GetResoluteOrMeltdownModel(Player player)
    {
        return ActiveRM[player] ?? RMModelDb.ResoluteOrMeltdown<NoResoluteAndMeltdown>();
    }

    public static bool IsInResoluteOrMeltdown<T>(Player player) where T : ResoluteOrMeltdownModel
    {
        return ActiveRM[player] is T;
    }


    public static async Task SetResoluteOrMeltdown<T>(PlayerChoiceContext ctx, Player player, CardModel? source) where T : ResoluteOrMeltdownModel
    {
        await SetResoluteOrMeltdown(ctx, player, RMModelDb.ResoluteOrMeltdown<T>(), source);
    }

    public static async Task SetResoluteOrMeltdown(PlayerChoiceContext ctx, Player player, ResoluteOrMeltdownModel newCanonical, CardModel? source)
    {
        var current = ActiveRM[player];
        //可以重复进入美德/折磨
        //if (current?.GetType() == newCanonical.GetType()) return;

        if (current != null)
            await current.OnExitResoluteOrMeltdown(ctx, player, source);

        var mutable = newCanonical.ToMutable(player);
        ActiveRM[player] = mutable;
        await mutable.OnEnterResoluteOrMeltdown(ctx, player, source);

        //var creatureNode = NCombatRoom.Instance?.GetCreatureNode(player.Creature);
        await DD2Hooks.OnResoluteOrMeltdownChanged(ctx, player, current!, ActiveRM[player]!);
    }
}