using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace DD2ModConfig.Code.Monster.DeathMonster;

public class DeathListenForCombatStateSingleton : CustomSingletonModel
{
    public DeathListenForCombatStateSingleton() : base(HookType.Combat)
    {

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
}