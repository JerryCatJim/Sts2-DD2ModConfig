using DD2ModConfig.Code.Compatibility;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;

namespace DD2ModConfig.Code.Helper;

public static class DD2RngHelper
{
    public static int GetRandomIndex(Player player, int N = 100)
    {
        if (player == null) return -1;

        int playerMaxHP = player.Creature?.MaxHp ?? 0;
        int floorCount = player.RunState?.TotalFloor ?? 0;
        int roundNumber = player.Creature?.CombatState?.RoundNumber ?? 0;
        return GetRandomIndex(player, playerMaxHP, floorCount, roundNumber, N);
    }
    public static int GetRandomIndex(ICombatState? combatState, int N = 100)
    {
        IRunState? runState = combatState?.RunState;
        if (combatState == null || runState == null) return -1;

        int AllPlayersHP = combatState.Players.Sum((Player p) => p.Creature?.CurrentHp ?? 0);
        int AllEnemiesHP = combatState.HittableEnemies.Sum((Creature c) => c?.CurrentHp ?? 0);
        return GetRandomIndex(runState, AllPlayersHP, AllEnemiesHP, runState.TotalFloor, N);
    }

    public static int GetRandomIndex(Player player, int a, int b, int c, int N)
    {
        if (N <= 0 || player == null)
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

    public static int GetRandomIndex(IRunState? runState, int a, int b, int c, int N)
    {
        if (N <= 0 || runState == null)
        {
            return -1;
        }

        ulong seed = DD2SeedCompat.GetRunRngSeed(runState.Rng);         //盐值，增加分散度

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
