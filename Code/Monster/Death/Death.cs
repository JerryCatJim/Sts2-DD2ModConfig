using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using DD2ModConfig.Code.Abstract;
using DD2ModConfig.Code.Audio;
using DD2ModConfig.Code.Config;
using DD2ModConfig.Code.Helper;
using DD2ModConfig.Code.Powers;
using DD2ModConfig.Code.Relics;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace DD2ModConfig.Code.Monster.DeathMonster;

public class Death : CustomMonsterModel, IGetDD2MonsterType
{
    public string TryGetMonsterType()
    {
        return "Death";
    }
    int CurrentActIndex => DeathListenForRunStateSingleton.CombatState?.RunState.CurrentActIndex ?? 0; //从0开始
    // 根据进阶提高最小和最大血量，进阶8及以上为A，否则为B
    public override int MinInitialHp => (AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 110, 80) + ExtraHp) * HpMultiple;
    public override int MaxInitialHp => (AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 120, 90) + ExtraHp) * HpMultiple;
    private int ExtraHp
    {
        get
        {
            if (CurrentActIndex <= 0) { return 0; }
            //第二层多40血，第三层多40+50血，第四层多40+50+60血，以此类推
            return CurrentActIndex * 5 * (CurrentActIndex + 7);
        }
    }
    private int CurrentAppearedTimes => DeathListenForRunStateSingleton.DeathAppearTime <= 0 ? 1 : DeathListenForRunStateSingleton.DeathAppearTime + 1;
    private bool ShouldEnhance => CurrentAppearedTimes >= 2 && DeathListenForRunStateSingleton.ShouldEnhanceDeathAfterDefeat;
    private int HpMultiple => ShouldEnhance ? CurrentAppearedTimes : 1;
    private int ExtraEnhancedValue => CurrentAppearedTimes >= 2 && ShouldEnhance ? CurrentAppearedTimes : 0;

    // 意图的数值，进阶9提高
    private int MementoMoriDoomNum => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 9) + CurrentActIndex * 2 + ExtraEnhancedValue;
    private int WaningCrescentDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 10) + CurrentActIndex * 2;
    private int SoulReaverDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 20, 16) + CurrentActIndex * 2;
    private int TrampleDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 6) + CurrentActIndex * 2;
    private int TrampleStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 3) + CurrentActIndex * 1 + ExtraEnhancedValue;
    private bool ShouldApplyVulunerable => CurrentActIndex >= 2 || ShouldEnhance; //(第三层出现时难逃一死给易伤)
    private bool ShouldApplyWeak => CurrentActIndex >= 0 || ShouldEnhance; //(第一层出现时践踏给虚弱)
    private bool ShouldApplyCombo => CurrentActIndex >= 1 || ShouldEnhance; //(第二层出现时难逃一死给破绽)
    //private bool ShouldReduceStrengthAndDexterity => CurrentActIndex >= 1; //(第二层出现时难逃一死削力量和敏捷)
    private int DeathArmorNum => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 3, 2);

    private int MementoMoriUsedTimes { get; set; } = 0;

    // 怪物场景，如果你的场景没有挂载脚本，参考这个
    public override NCreatureVisuals? CreateCustomVisuals() => NodeFactory<NCreatureVisuals>.CreateFromScene("res://DD2ModConfig_Asset/Monster_Death/Death.tscn");

    // 如果你挂载了自己的自定义脚本，使用这个(挂了也用上面的，这个会卡死)
    //public override string? CustomVisualPath => "res://DD2ModConfig_Asset/Monster_Death/Death.tscn";

    public bool ShouldPlayBgm = false;
    // 战斗开始时，在这里给自己上buff之类
    public override async Task AfterAddedToRoom()
    {
        NCreature? DeathNode = NCombatRoom.Instance?.GetCreatureNode(base.Creature);
        if (DeathNode != null && Creature.SlotName == null)
        {
            DeathNode.Visible = false;
            DeathNode.GlobalPosition = new Vector2(1417, 739);
            if (Creature.CombatState != null)
            {
                NCreature? playerNode = NCombatRoom.Instance?.GetCreatureNode(LocalContext.GetMe(Creature.CombatState)?.Creature);
                if (playerNode != null)
                {
                    float newPosX = Math.Min(playerNode.GlobalPosition.X + 900, 1480);
                    DeathNode.GlobalPosition = new Vector2(newPosX, playerNode.GlobalPosition.Y);
                }
            }
            //await Cmd.CustomScaledWait(0.1f, 0.1f);
            //DeathNode.Visible = true;
        }

        if (DeathListenForRunStateSingleton.IsDeathExistingInCombat == false)
        {
            //若多个死神同时出现只加载一次BGM
            if (DD2ModConfigCfg.ShouldPlayDeathEncounterBgm)
            {
                //DD2AudioManager.PlayDD2Bgm("Death");
                ShouldPlayBgm = true;
            }
        }
        DeathListenForRunStateSingleton.IsDeathExistingInCombat = true;
        //MonsterAudioManager.PlayDeathSfx("Spawn", true);

        await PowerCmd.Apply<DeathArmorPower>(new ThrowingPlayerChoiceContext(), Creature, DeathArmorNum, Creature, null, true);
        if (ExtraEnhancedValue > 0)
        {
            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Creature, ExtraEnhancedValue, Creature, null, true);
        }
    }

    public async Task SetVisibleAndPlaySpawnSfx()
    {
        NCreature? DeathNode = NCombatRoom.Instance?.GetCreatureNode(base.Creature);
        if (DeathNode != null)
        {
            await CreatureCmd.TriggerAnim(Creature, "Spawn", 0.1f);
            //给点延迟再恢复可见，防止视觉上突然闪一下
            DeathNode.Visible = true;

            if (ShouldPlayBgm)
            {
                DD2AudioManager.PlayDD2Bgm("Death");
            }
            MonsterAudioManager.PlayDeathSfx("Spawn", true);
        }
    }

    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        //防止播放死神聚焦效果时，玩家没等到播完立刻结束了自己回合而来到了怪物回合，此时应恢复镜头到原位
        if (participants.Contains(Creature))
        {
            DD2MonsterHelper.ClearAllTweens();
        }
        return Task.CompletedTask;
    }

    public override async Task BeforeDeath(Creature creature)
    {
        if (creature != Creature || CombatState == null) return;
        if (creature.MaxHp > 0 && !Hook.ShouldDie(CombatState.RunState, CombatState, creature, out _)) return;

        //奖励生成别放在AfterDeath，多人模式会不生效（可能是结算顺序问题）
        AbstractRoom? currentRoom = CombatState.RunState.CurrentRoom;
        if (currentRoom is CombatRoom combatRoom)
        {
            foreach (var player in combatRoom.CombatState.Players)
            {
                PotionReward potionReward = new PotionReward(player);
                combatRoom.AddExtraReward(player, new CardReward(CardCreationOptions.ForRoom(player, RoomType.Boss), 3, player));
                combatRoom.AddExtraReward(player, new RelicReward(GetRandomDeathHeadReward(player), player));
                combatRoom.AddExtraReward(player, potionReward);
                combatRoom.AddExtraReward(player, new GoldReward(66, player));
            }
        }

        MonsterAudioManager.PlayDeathSfx("Dead", true);
        await CreatureCmd.TriggerAnim(Creature, "Dead", 0);
        //await Cmd.CustomScaledWait(2f, 2f, true);

        DeathListenForRunStateSingleton.IsDeathExistingInCombat = CombatState.HittableEnemies.Any((Creature c) => c.IsAlive && c.Monster is Death);
        if (DeathListenForRunStateSingleton.IsDeathExistingInCombat == false)
        {
            DeathListenForRunStateSingleton.DeathAppearTime++;

            //防止死神没播完出场聚焦效果时就已经死亡(正常游玩过程中不可能发生)
            DD2MonsterHelper.ClearAllTweens();

            await Cmd.CustomScaledWait(1f, 1f, true);
            DD2MonsterHelper.ClearDeathSpawnVfxAndBgm();
            await Cmd.CustomScaledWait(1f, 1f, true);
        }
    }

/*#if STS2_AT_LEAST_0_113_0
    public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature target, CardModel? source, bool wasRemovalPrevented, float deathAnimLength)
#elif STS2_AT_LEAST_0_111_0
    public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature target, bool wasRemovalPrevented, float deathAnimLength)
#endif
    {
        if (target != Creature || CombatState == null) return Task.CompletedTask;
        if (target.MaxHp > 0 && !Hook.ShouldDie(CombatState.RunState, CombatState, target, out _)) return Task.CompletedTask;

        DeathListenForRunStateSingleton.IsDeathExistingInCombat = CombatState.HittableEnemies.Any((Creature c) => c.IsAlive && c.Monster is Death);
        if (DeathListenForRunStateSingleton.IsDeathExistingInCombat == false)
        {
            DeathListenForRunStateSingleton.DeathAppearTime++;
            DD2MonsterHelper.ClearDeathSpawnVfxAndBgm();
        }
        return Task.CompletedTask;
    }*/

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        // 你也可以创建RandomBranchState（随机意图分支）和ConditionalBranchState（条件意图分支）来实现更复杂的状态转换逻辑
        var MementoMori = new MoveState(
            "MEMENTO_MORI",
            MementoMoriMove,
            new DebuffIntent(true)
            );
        var SoulReaver = new MoveState(
            "SOUL_REAVER",
            SoulReaverMove,
            new SingleAttackIntent(SoulReaverDamage)
            );
        var WaningCrescent = new MoveState(
            "WANING_CRESCENT",
            WaningCrescentMove,
            new SingleAttackIntent(WaningCrescentDamage),
            new DefendIntent()
            );
        var Trample = new MoveState(
            "TRAMPLE",
            TrampleMove,
            new SingleAttackIntent(TrampleDamage),
            new BuffIntent(),
            new DebuffIntent()
            );

        MementoMori.FollowUpState = SoulReaver;
        SoulReaver.FollowUpState = WaningCrescent;
        WaningCrescent.FollowUpState = Trample;
        Trample.FollowUpState = MementoMori;

        return new MonsterMoveStateMachine([MementoMori, SoulReaver, WaningCrescent, Trample], MementoMori);
    }

    private async Task MementoMoriMove(IReadOnlyList<Creature> targets)
    {
        MementoMoriUsedTimes++;

        await CreatureCmd.TriggerAnim(Creature, "Attack/Attack_Point", 0);
        await Cmd.CustomScaledWait(1.5f, 1.5f);
        NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Normal, 180f + MegaCrit.Sts2.Core.Random.Rng.Chaotic.NextFloat(-10f, 10f));
        foreach (Creature creature in targets)
        {
            if (creature == null || creature.IsDead) continue;

            //玩家反馈 : 对战活雾时被死神入侵会导致活雾的debuff未被消除而难以战斗
            if (MementoMoriUsedTimes == 1)
            {
                foreach (PowerModel p in creature.Powers.Where(
                       p => p is not DoomPower
                       && p.Type == MegaCrit.Sts2.Core.Entities.Powers.PowerType.Debuff
                       && p.GetType().Namespace == "MegaCrit.Sts2.Core.Models.Powers"
                       ).ToList())
                {
                    await PowerCmd.Remove(p);
                }
            }

            //多人模式给已经阵亡的玩家施加Doom会导致活人的Doom无法结算
            await PowerCmd.Apply<DoomPower>(new ThrowingPlayerChoiceContext(), creature, MementoMoriDoomNum, Creature, null);
            if (ShouldApplyCombo || IsFlagellant(creature))
            {
                await PowerCmd.Apply<ComboPower>(new ThrowingPlayerChoiceContext(), creature, 1, Creature, null);
            }
            if (ShouldApplyVulunerable || IsFlagellant(creature))
            {
                await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), creature, 2, Creature, null);
            }
            /*if (ShouldReduceStrengthAndDexterity || IsDD2ModConfig(creature))
            {
                await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), creature, -1, Creature, null);
                await PowerCmd.Apply<DexterityPower>(new ThrowingPlayerChoiceContext(), creature, -1, Creature, null);
            }*/
            await PowerCmd.Apply<StressPower>(new ThrowingPlayerChoiceContext(), creature, MementoMoriUsedTimes, Creature, null);
        }
    }

    private async Task SoulReaverMove(IReadOnlyList<Creature> targets)
    {
        // 说话
        //TalkCmd.Play(L10NMonsterLookup("DD2MODCONFIG-DEATH.moves.SOUL_REAVER.banter"), Creature, VfxColor.Blue);
        await DamageCmd
            .Attack(SoulReaverDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack/Attack_C", 0f) //原版其实是Attack_B，为了视觉冲击力，更有力量感，调换了一下动画
            .AfterAttackerAnim(delegate
            {
                NGame.Instance?.ScreenShake(ShakeStrength.Medium, ShakeDuration.Normal, 180f + MegaCrit.Sts2.Core.Random.Rng.Chaotic.NextFloat(-10f, 10f));
                return Task.CompletedTask;
            })
            .WithWaitBeforeHit(1.2f, 1.2f)
            .WithHitFx("vfx/vfx_attack_slash") // 攻击特效
            .Execute(null);

        foreach (Creature creature in targets)
        {
            if (creature == null || creature.IsDead) continue;
            await PowerCmd.Apply<StressPower>(new ThrowingPlayerChoiceContext(), creature, 2, Creature, null);
        }
    }

    private async Task WaningCrescentMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(WaningCrescentDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack/Attack_B", 0f) //原版其实是Attack_C
            .AfterAttackerAnim(delegate
            {
                NGame.Instance?.ScreenShake(ShakeStrength.Strong, ShakeDuration.Normal, 180f + MegaCrit.Sts2.Core.Random.Rng.Chaotic.NextFloat(-10f, 10f));
                return Task.CompletedTask;
            })
            .WithWaitBeforeHit(1.2f, 1.2f)
            .WithHitFx("vfx/vfx_attack_slash") // 攻击特效
            .Execute(null);
        //多人模式 游戏会自动将格挡按人数增值
        await CreatureCmd.GainBlock(Creature, WaningCrescentDamage + ExtraEnhancedValue, ValueProp.Move, null);
    }

    private async Task TrampleMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(TrampleDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack/Attack_Trample", 0f)
            .WithWaitBeforeHit(1.2f, 1.2f)
            .WithHitFx("vfx/vfx_attack_blunt") // 攻击特效
            .Execute(null);
        foreach (Creature creature in targets)
        {
            if (creature == null || creature.IsDead) continue;
            if (creature.GetPower<ComboPower>() is ComboPower comboP)
            {
                NGame.Instance?.ScreenShake(ShakeStrength.Strong, ShakeDuration.Normal, 180f + MegaCrit.Sts2.Core.Random.Rng.Chaotic.NextFloat(-10f, 10f));
                await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), comboP, -1, creature, null);
                await PowerCmd.Apply<RingingPower>(new ThrowingPlayerChoiceContext(), creature, 1m, Creature, null);
            }
            if (ShouldApplyWeak || IsFlagellant(creature))
            {
                await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), creature, 2, Creature, null);
            }
            await PowerCmd.Apply<StressPower>(new ThrowingPlayerChoiceContext(), creature, 2, Creature, null);
        }
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Creature, TrampleStrength, Creature, null);
    }

    private bool IsFlagellant(Creature creature)
    {
        return DD2Helper.IsFlagellant(creature);
    }
    private RelicModel GetRandomDeathHeadReward(Player player)
    {
        if (player == null) return ModelDb.Relic<DeathsHead>().ToMutable();

        int index = player.PlayerRng.Rewards.NextInt(0, 3);
        return index switch
        {
            0 => ModelDb.Relic<DeathsHead>().ToMutable(),
            1 => ModelDb.Relic<DeathsHead_Damage>().ToMutable(),
            2 => ModelDb.Relic<DeathsHead_Block>().ToMutable(),
            _ => ModelDb.Relic<DeathsHead>().ToMutable(),
        };
    }
}