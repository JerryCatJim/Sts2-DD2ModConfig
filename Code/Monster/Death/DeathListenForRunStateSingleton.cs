using BaseLib.Abstracts;
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
using MegaCrit.Sts2.Core.Saves;

namespace DD2ModConfig.Code.Monster.DeathMonster;

public class DeathListenForRunStateSingleton : CustomSingletonModel
{
    public DeathListenForRunStateSingleton() : base(HookType.Run)
    {
        
    }

    public static bool ShouldPredictWhetherDeathWillAppear { get; set; } = DD2ModConfigCfg.PredictWhetherDeathWillAppear;
    public static bool ShouldDeathOnlyHuntFlagellant { get; set; } = DD2ModConfigCfg.ShouldDeathOnlyHuntFlagellant;
    public static int DeathAppearMaxTime { get; set; } = DD2ModConfigCfg.DeathAppearMaxTime;
    public static int DeathEncounterChance { get; set; } = DD2ModConfigCfg.DeathEncounterChance;
    public static bool ShouldDeathAppearInMonsterRoom { get; set; } = DD2ModConfigCfg.ShouldDeathAppearInMonsterRoom;
    public static bool ShouldDeathAppearInEliteRoom { get; set; } = DD2ModConfigCfg.ShouldDeathAppearInEliteRoom;
    public static bool ShouldDeathAppearInBossRoom { get; set; } = DD2ModConfigCfg.ShouldDeathAppearInBossRoom;
    public static bool ShouldEnhanceDeathAfterDefeat { get; set; } = DD2ModConfigCfg.ShouldEnhanceDeathAfterDefeat;

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

    public static void RefreshValueByLocalConfig()
    {
        ShouldPredictWhetherDeathWillAppear = DD2ModConfigCfg.PredictWhetherDeathWillAppear;
        ShouldDeathOnlyHuntFlagellant = DD2ModConfigCfg.ShouldDeathOnlyHuntFlagellant;
        DeathAppearMaxTime = DD2ModConfigCfg.DeathAppearMaxTime;
        DeathEncounterChance = DD2ModConfigCfg.DeathEncounterChance;
        ShouldDeathAppearInMonsterRoom = DD2ModConfigCfg.ShouldDeathAppearInMonsterRoom;
        ShouldDeathAppearInEliteRoom = DD2ModConfigCfg.ShouldDeathAppearInEliteRoom;
        ShouldDeathAppearInBossRoom = DD2ModConfigCfg.ShouldDeathAppearInBossRoom;
        ShouldEnhanceDeathAfterDefeat = DD2ModConfigCfg.ShouldEnhanceDeathAfterDefeat;
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

            ShouldSpawnDeathThisRoom = CheckSpawnDeathCondition(combatRoom);
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
            if (ShouldDeathOnlyHuntFlagellant && !combatRoom.CombatState.Players.Any((Player p) => DD2Helper.IsFlagellant(p)))
            {
                return false;
            }

            if (combatRoom.Encounter.IsWeak || DeathAppearTime >= DeathAppearMaxTime)
            {
                return false;
            }

            int EncounterChance = Math.Clamp(DeathEncounterChance, 0, 100);
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
        return (ShouldDeathAppearInMonsterRoom && room.RoomType == RoomType.Monster)
            || (ShouldDeathAppearInEliteRoom && room.RoomType == RoomType.Elite)
            || (ShouldDeathAppearInBossRoom && room.RoomType == RoomType.Boss);
    }
}