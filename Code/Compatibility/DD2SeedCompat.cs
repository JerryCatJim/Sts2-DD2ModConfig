using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using System.Reflection;

namespace DD2ModConfig.Code.Compatibility;

public static class DD2SeedCompat
{
    private static readonly PropertyInfo? RunRngSeed =
        typeof(RunRngSet).GetProperty("Seed",
            BindingFlags.Public | BindingFlags.Instance);
    private static readonly PropertyInfo? PlayerRngSetSeed =
        typeof(PlayerRngSet).GetProperty("Seed",
            BindingFlags.Public | BindingFlags.Instance);

    public static ulong GetRunRngSeed(RunRngSet rng)
    {
        if (RunRngSeed == null) return 0;

        return RunRngSeed.GetValue(rng) switch
        {
            uint u => u,          // 0.107
            ulong ul => ul,         // 0.111
            _ => 0
        };
    }
    public static ulong GetPlayerRngSetSeed(PlayerRngSet rng)
    {
        if (PlayerRngSetSeed == null) return 0;

        return PlayerRngSetSeed.GetValue(rng) switch
        {
            uint u => u,          // 0.107
            ulong ul => ul,         // 0.111
            _ => 0
        };
    }
}
