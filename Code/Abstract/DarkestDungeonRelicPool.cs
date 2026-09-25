using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;

namespace DD2ModConfig.Code.Abstract;

public partial class DarkestDungeonRelicPool : CustomRelicPoolModel
{
    public override bool IsShared => true;
    //public override bool SeenByDefault => true;

    //标记了Pool特性无需手动填充
    /*protected override IEnumerable<RelicModel> GenerateAllRelics()
    {
        return new List<RelicModel>
        {
            ModelDb.Relic<StressCounterRelic>(),
            ModelDb.Relic<DeathsHead>(),
        };
    }*/

    public override IEnumerable<RelicModel> GetUnlockedRelics(UnlockState unlockState)
    {
        List<RelicModel> list = base.AllRelics.ToList();
        return list;
    }
}
