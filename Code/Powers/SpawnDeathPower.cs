using DD2ModConfig.Code.Abstract;
using DD2ModConfig.Code.Monster.DeathMonster;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace DD2ModConfig.Code.Monster;

public sealed class SpawnDeathPower : DD2ModConfigPowerModel
{
    public override PowerType Type => PowerType.None;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override bool IsVisibleInternal => DeathListenForRunStateSingleton.ShouldPredictWhetherDeathWillAppear;

    private bool ShouldSpawnDeath { get; set; } = false;
    private IEnumerable<Creature>? UnremovedCreatures { get; set; }

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature target, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (!wasRemovalPrevented && target == base.Owner && target.IsPrimaryEnemy && target.Monster is not Death
            && !target.Powers.Any((PowerModel p) => p is not SpawnDeathPower && p.ShouldStopCombatFromEnding())
            && target.CombatState != null)
        {
            ShouldSpawnDeath = !target.CombatState.Enemies.Any((Creature c) => c.IsPrimaryEnemy && c.IsAlive)
                && !target.CombatState.Enemies.Any((Creature c) => c.IsPrimaryEnemy && 
                        c.Powers.Any((PowerModel p) => p is not SpawnDeathPower && p.ShouldStopCombatFromEnding()));

            if (ShouldSpawnDeath)
            {
                //如果爪牙存活时击杀了主人，发现爪牙不会自动死亡，所以清理一下
                await CreatureCmd.Kill(target.CombatState.HittableEnemies.Where((Creature c) => c.IsSecondaryEnemy).ToList());//Kill(CombatState.Enemies)会直接闪退?

                //拥有ShouldCreatureBeRemovedFromCombatAfterDeath的power的怪，彻底死后需要在CombatState将其移除才能让与死神的战斗在结束后正常结算
                UnremovedCreatures = target.CombatState.Enemies.
                    Where((Creature creature) => creature != null && creature.IsDead 
                        && creature.Powers.Any((PowerModel p) => !p.ShouldCreatureBeRemovedFromCombatAfterDeath(creature))).
                    ToList();
                foreach (Creature creature in UnremovedCreatures)
                {
                    NCreature? nCreature = NCombatRoom.Instance?.GetCreatureNode(creature);
                    if (nCreature != null)
                    {
                        nCreature.AnimHideIntent();
                        nCreature.DeathAnimationTask = TaskHelper.RunSafely(DeleteNCreature(nCreature));
                        nCreature.StartDeathAnim(true);
                        NCombatRoom.Instance?.RemoveCreatureNode(nCreature);
                    }
                }

                await Cmd.CustomScaledWait(2.5f, 2.5f);
                Death DeathMonster = (Death)ModelDb.Monster<Death>().ToMutable();
                await CreatureCmd.Add(DeathMonster, target.CombatState, target.Side, null);

                //若在死神生成前清除CombatState残留怪物会生不出死神?
                //直接在这里清除残留enemy会导致若排在SpawnDeathPower之后的Power里的AfterDeath想使用creature以及creature.CombatState时有概率因报空而报错卡死
                //例如和yuwan的猪MOD开了猪进阶一起使用时，若怪物因中毒死亡而召唤死神时，会无法进行下一回合
                //所以为了兼容起见，把清除代码挪到AfterRemoved里了
                /*foreach (Creature creature in UnremovedCreatures)
                {
                    if (creature.Side == CombatSide.Enemy && (creature.CombatState?.Enemies.Contains(creature) ?? false))
                    {
                        CombatManager.Instance.RemoveCreature(creature);
                        MonsterModel? monster = creature.Monster;
                        if (monster != null && !monster.IsPerformingMove)
                        {
                            creature.CombatState.RemoveCreature(creature);
                        }
                    }
                }*/
            }
        }
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        if (ShouldSpawnDeath && oldOwner != null && UnremovedCreatures != null)// && oldOwner.CombatState != null)  //oldOwner的CombatState为空
        {
            //直接在AfterDeath清除残留enemy会导致若排在SpawnDeathPower之后的Power里的AfterDeath想使用creature以及creature.CombatState时有概率因报空而报错卡死
            //例如和yuwan的猪MOD开了猪进阶一起使用时，若怪物因中毒死亡而召唤死神时，会无法进行下一回合
            //所以为了兼容起见，把清除代码挪到AfterRemoved里了
            foreach (Creature creature in UnremovedCreatures)
            {
                if (creature == null) continue;
                if (creature.Side == CombatSide.Enemy && (creature.CombatState?.Enemies.Contains(creature) ?? false))
                {
                    CombatManager.Instance.RemoveCreature(creature);
                    MonsterModel? monster = creature.Monster;
                    if (monster != null && !monster.IsPerformingMove)
                    {
                        creature.CombatState.RemoveCreature(creature);
                    }
                }
            }
            UnremovedCreatures = null;
        }
        return Task.CompletedTask;
    }

    private async Task DeleteNCreature(NCreature nCreature)
    {
        if (nCreature == null) return;

        await Cmd.Wait(0.25f, ignoreCombatEnd: true);
        nCreature.QueueFreeSafely();
    }

    public override bool ShouldStopCombatFromEnding()
    {
        return true;
    }
    public override bool ShouldPowerBeRemovedAfterOwnerDeath()
    {
        return ShouldSpawnDeath;
    }
}
