using BaseLib.Abstracts;
using DD2ModConfig.Code.Compatibility;
using DD2ModConfig.Code.Config;
using DD2ModConfig.Code.Helper;
using DD2ModConfig.Code.Relics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace DD2ModConfig.Code.Monster.DeathMonster;

public class DeathListenForRunStateSingleton : CustomSingletonModel
{
    public DeathListenForRunStateSingleton() : base(HookType.Run)
    {
        
    }

    private static bool UseMultiplayerDefaultCondition => CombatState?.Players.Count > 1 && DD2ModConfigCfg.ShouldMultiplayerUseDefaultCondition;
    public static bool ShouldPredictWhetherDeathWillAppear => UseMultiplayerDefaultCondition ? true : DD2ModConfigCfg.PredictWhetherDeathWillAppear;
    public static int DeathAppearTime { get; set; } = 0;
    public static bool ShouldSpawnDeathThisRoom { get; set; } = false;

    public static CombatState? CombatState { get; set; }

    private static int _hitCount = 0;
    public static int HitCount
    {
        get
        {
            return _hitCount;
        }
        set
        {
            _hitCount = value % 5;
        }
    }

    public static bool IsDeathExistingInCombat { get; set; } = false;

    public static void ResetValue(bool ShouldClearDeathAppearTime = true)
    {
        DeathAppearTime = ShouldClearDeathAppearTime ? 0 : DeathAppearTime;
        ShouldSpawnDeathThisRoom = false;
        CombatState = null;
        HitCount = 0;
        IsDeathExistingInCombat = false;
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        NAudioManager.Instance?.SetBgmVol(SaveManager.Instance.SettingsSave.VolumeBgm);

        ResetValue(false);
        if (room is CombatRoom combatRoom)
        {
            CombatState = combatRoom.CombatState;
            //优化一下死神出现次数的计数逻辑 ：改为取房间内拥有死神之颅数量最多的玩家的死神之颅拥有数(因为击败一次死神掉一个这个遗物)
            //以便中途关闭游戏后再打开游戏进程可以正确计数
            //潜在问题：如果击败死神后没拾取其遗物，退出进程后重进游戏会少次数（但应该不会有人不捡吧？实在不想搞个空的纯计数遗物SaveProperty了）
            DeathAppearTime = CombatState?.Players.Max((Player p) => p.Relics.Count((RelicModel r) => r is DeathsHead)) ?? 0;

            IsDeathExistingInCombat = CombatState?.HittableEnemies.Any((Creature c) => c.IsMonster && c.Monster is Death) ?? false;

            ShouldSpawnDeathThisRoom = UseMultiplayerDefaultCondition ? 
                CheckSpawnDeathConditionForMultiplayerDefault(combatRoom)
                : CheckSpawnDeathCondition(combatRoom);
        }
        return Task.CompletedTask;
    }

    public override async Task BeforeCombatStart()
    {
        if (ShouldSpawnDeathThisRoom && CombatState != null)
        {
            //一定要在怪物初始化之后再应用，以便最后结算生成死神。
            //若最先结算，例如千足虫等拥有ShouldCreatureBeRemovedFromCombatAfterDeath()且死后特殊处理的怪会出很多问题
            foreach(Creature creature in CombatState.HittableEnemies)
            {
                if(creature != null && !creature.HasPower<SpawnDeathPower>())
                {
                    await PowerCmd.Apply<SpawnDeathPower>(new ThrowingPlayerChoiceContext(), creature, 1, null, null, true);
                }
            }
        }
    }

    private bool CheckSpawnDeathCondition(AbstractRoom room)
    {
        if (room is CombatRoom combatRoom)
        {
            if (DD2ModConfigCfg.ShouldDeathOnlyHuntFlagellant
                && !combatRoom.CombatState.Players.Any((Player p) => DD2Helper.IsFlagellant(p)))
            {
                return false;
            }

            if (combatRoom.Encounter.IsWeak || DeathAppearTime >= DD2ModConfigCfg.DeathAppearMaxTime)
            {
                return false;
            }

            int EncounterChance = Math.Clamp(DD2ModConfigCfg.DeathEncounterChance, 0, 100);
            if (EncounterChance <= 0)
            {
                return false;
            }

            if (CheckCombatRoomTypeForSpawningDeath(combatRoom))
            {
                //按理说应该只让这个Rng方法在所有玩家内只执行一次，多次执行会多人模式数据不同步，但是我没找到好的位置，所以自己写个哈希凑合一下
                //int index0 = combatRoom.CombatState.RunState.Rng.UpFront.NextInt(0, 99);
                ICombatState combatState = combatRoom.CombatState;
                int index = DD2RngHelper.GetRandomIndex(combatState, 100);
                if (index >= 0 && index < EncounterChance)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool CheckCombatRoomTypeForSpawningDeath(CombatRoom room)
    {
        return (DD2ModConfigCfg.ShouldDeathAppearInMonsterRoom && room.RoomType == RoomType.Monster)
            || (DD2ModConfigCfg.ShouldDeathAppearInEliteRoom && room.RoomType == RoomType.Elite)
            || (DD2ModConfigCfg.ShouldDeathAppearInBossRoom && room.RoomType == RoomType.Boss);
    }

    private bool CheckSpawnDeathConditionForMultiplayerDefault(AbstractRoom room)
    {
        if (room is CombatRoom combatRoom)
        {
            if (!combatRoom.CombatState.Players.Any((Player p) => DD2Helper.IsFlagellant(p)))
            {
                return false;
            }

            if (combatRoom.Encounter.IsWeak || DeathAppearTime >= 1 )
            {
                return false;
            }

            if (combatRoom.RoomType == RoomType.Monster)
            {
                //按理说应该只让这个Rng方法在所有玩家内只执行一次，多次执行会多人模式数据不同步，但是我没找到好的位置，所以自己写个哈希凑合一下
                //int index0 = combatRoom.CombatState.RunState.Rng.UpFront.NextInt(0, 99);
                ICombatState combatState = combatRoom.CombatState;
                int index = DD2RngHelper.GetRandomIndex(combatState, 100);
                if (index >= 0 && index < 6)
                {
                    return true;
                }
            }
        }
        return false;
    }
}