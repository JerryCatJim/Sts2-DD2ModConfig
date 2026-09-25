using DD2ModConfig.Code.Helper;
using DD2ModConfig.Code.ResoluteOrMeltdown;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;

namespace DD2ModConfig.Code.Core;
internal class RMSubscriber
{
    public static void Subscribe()
    {
        ModHelper.SubscribeForCombatStateHooks(MainFile.ModId, CollectModels);
    }

    public static IEnumerable<AbstractModel> CollectModels(CombatState combatState)
    {
        return combatState.Players
            .Select(RMHelper.GetResoluteOrMeltdownModel)
            .Where(s => s is not NoResoluteAndMeltdown);
    }
}
