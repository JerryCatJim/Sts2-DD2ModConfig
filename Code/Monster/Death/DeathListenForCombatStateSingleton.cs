using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace DD2ModConfig.Code.Monster.DeathMonster;

public class DeathListenForCombatStateSingleton : CustomSingletonModel
{
    public DeathListenForCombatStateSingleton() : base(HookType.Combat)
    {

    }

    public static Vector2 CameraOriginalPos = Vector2.Zero;
    public static Vector2 CameraOriginalScale = Vector2.One;
    public static bool HasDeathSpawnVfx = false;
    public static void ResetValue()
    {
        HasDeathSpawnVfx = false;
        CameraOriginalPos = Vector2.Zero;
        CameraOriginalScale = Vector2.One;
    }

    public override async Task AfterCreatureAddedToCombat(Creature creature)
    {
        if (!DeathListenForRunStateSingleton.ShouldSpawnDeathThisRoom) return;

        if (creature.IsMonster && creature.Side != MegaCrit.Sts2.Core.Combat.CombatSide.Player
            && creature.Monster is not Death)
        {
            await PowerCmd.Apply<SpawnDeathPower>(new ThrowingPlayerChoiceContext(), creature, 1, null, null, true);
        }
    }
    public override Task BeforeCombatStart()
    {
        Control? layer = NCombatRoom.Instance?.SceneContainer;
        if (layer != null)
        {
            CameraOriginalPos = layer.Position;
            CameraOriginalScale = layer.Scale;
        }
        return Task.CompletedTask;
    }
}