using BaseLib.Abstracts;
using DD2ModConfig.Code.Helper;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Rooms;

namespace DD2ModConfig.Code.Singleton;

public class DD2RunstateSingleton : CustomSingletonModel
{
    public DD2RunstateSingleton() : base(HookType.Run)
    {

    }

    public static CombatState? CombatState { get; set; }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        DD2CombatSingleton.ResetValueBeforeCombat(); 
        DD2Helper.ResetCombatDictionaries();

        if (room is CombatRoom combatRoom)
        {
            CombatState = combatRoom.CombatState;
        }
        return Task.CompletedTask;
    }

    public override Task BeforeCombatStart()
    {
        if (CombatState == null) return Task.CompletedTask;

        RMHelper.InitActiveRM(CombatState);

        return Task.CompletedTask;
    }
}