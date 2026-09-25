using BaseLib.Abstracts;
using DD2ModConfig.Code.Abstract;
using DD2ModConfig.Code.Config;
using DD2ModConfig.Code.Helper;
using DD2ModConfig.Code.Powers;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Runs;

namespace DD2ModConfig.Code.Singleton;

public class DD2CombatSingleton : CustomSingletonModel, IAfterStressChanged, IAfterDeathDoor, IBeforePoisonTrigger
{
    public DD2CombatSingleton() : base(HookType.Combat)
    {

    }

    public static Dictionary<ulong, decimal> GainedStressDictionary = new Dictionary<ulong, decimal> { };
    public static Dictionary<ulong, int> StressLockDictionary = new Dictionary<ulong, int> { };
    public Task AfterStressAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (amount > 0 && power != null && power.Owner != null && power.Owner.Player != null)
        {
            if (!GainedStressDictionary.TryAdd(power.Owner.Player.NetId, amount))
            {
                // 添加失败，说明键已存在，手动更新
                GainedStressDictionary[power.Owner.Player.NetId] += amount;
            }
        }
        return Task.CompletedTask;
    }
    public static void ResetValueBeforeCombat()
    {
        GainedStressDictionary.Clear();
        StressLockDictionary.Clear();
        _beforeTriggeredPoisons.Clear();
    }
    public static void ResetAllValue()
    {
        ResetValueBeforeCombat();
    }
    public override Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != null)
        { 
            StressLockDictionary[player.NetId] = 0; 
        }
        return Task.CompletedTask;
    }

    //用来记录需要在中毒造成伤害前广播触发中毒事件的poisonPower实例
    private static readonly Dictionary<Creature, PowerModel> _beforeTriggeredPoisons = new();
    public void BeforePoisonTrigger(PowerModel power, decimal amount)
    {
        if (power != null && power.Owner != null)
        {
            _beforeTriggeredPoisons[power.Owner] = power;
        }
    }

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (delta >= 0 || creature == null || creature.IsDead) return;

        //因为毒触发时是先造成伤害再减1层数，所以如果是毒造成的伤害，需要检测是否死于中毒时传入1层delta来预测减1层以正确计算
        decimal PoisonDelta = _beforeTriggeredPoisons.TryGetValue(creature, out var poisonPower) && poisonPower != null ? 1 : 0;
        
        if (DD2Helper.WillDieInPoison(creature, 0, PoisonDelta))
        {
            if (!DD2Helper.WillDieInPoison(creature, delta, 0))
            {
                //预计算的这一层PoisonDelta要不要广播出去？
                await BroadcastDeathDoorEvent(creature, delta, PoisonDelta, DeathDoorType.Poison);
            }
        }
        else if (DD2Helper.WillDieInDoom(creature))
        {
            if (!DD2Helper.WillDieInDoom(creature, delta, 0))
            {
                TryRegisterCreaturePosDoomed(creature);
                if (creature.CombatState?.CurrentSide == creature.Side)
                {
                    await BroadcastDeathDoorEvent(creature, delta, 0, DeathDoorType.Doom);
                }
            }
        }
        else if (DD2Helper.IsInDeathDoorHp(creature))
        {
            if (!DD2Helper.IsInDeathDoorHp(creature, delta)
                || ((100m * (creature.CurrentHp - delta) / creature.MaxHp) == DD2Helper.GetDeathDoorPercent(creature) && DD2Helper.GetDeathDoorPercent(creature) == 100m))
            {
                await BroadcastDeathDoorEvent(creature, delta, 0, DeathDoorType.LowHealth);
            }
        }
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power.Owner == null || power.Owner.IsDead) return;

        Creature creature = power.Owner;
        if (power is PoisonPower)
        {
            if (amount < 0m)
            {
                _beforeTriggeredPoisons.Remove(power.Owner);
                return;
            }
            if (DD2Helper.WillDieInPoison(creature))
            {
                if (!DD2Helper.WillDieInPoison(creature, 0, amount))
                {
                    await BroadcastDeathDoorEvent(creature, 0, amount, DeathDoorType.Poison);
                }
            }
        }
        else if (power is DoomPower)
        {
            if (DD2Helper.WillDieInDoom(creature) && amount > 0)
            {
                if (!DD2Helper.WillDieInDoom(creature, 0, amount))
                {
                    TryRegisterCreaturePosDoomed(creature);
                    if (creature.CombatState?.CurrentSide == creature.Side)
                    {
                        await BroadcastDeathDoorEvent(creature, 0, amount, DeathDoorType.Doom);
                    }
                }
            }
        }
    }

    private bool TryRegisterCreaturePosDoomed(Creature creature)
    {
        if (DD2Helper.WillDieInDoom(creature))
        {
            NCreature? nCreature = creature.GetCreatureNode();
            if (!(nCreature == null || !GodotObject.IsInstanceValid(nCreature) ||
                nCreature.Visuals == null || !GodotObject.IsInstanceValid(nCreature.Visuals)))
            {
                Vector2 globalPos = nCreature.Visuals.GetNodeOrNull<Marker2D>("%CenterPos")?.GlobalPosition ?? nCreature.GlobalPosition;
                return DD2Helper.RegisterCreaturePosDoomed(creature, globalPos);
            }
        }
        return false;
    }

    //在非生物本身的回合获得doom后不直接提示死门，该生物回合开始后若即将死于doom则提示死门
    public override async Task AfterSideTurnStartLate(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        foreach (Creature creature in participants)
        {
            if (DD2Helper.WillDieInDoom(creature))
            {
                TryRegisterCreaturePosDoomed(creature);
                await BroadcastDeathDoorEvent(creature, 0, 0, DeathDoorType.Doom);
            }
            else
            {
                DD2Helper.UnRegisterCreaturePosDoomed(creature);
            }
        }
    }

    public override async Task BeforeDeath(Creature creature)
    {
        if (creature == null) return;

        //用AfterDeath会导致PlayDeathVfx内部取不到NCreature，因为它在AfterDeath通知之前被清理了
        bool shouldDie = true;
        AbstractModel? preventer = null;
        if (creature.CombatState != null)
        {
            ICombatState combatState = creature.CombatState;
            IRunState runState = combatState.RunState;
            shouldDie = Hook.ShouldDie(runState, combatState, creature, out preventer);
        }
        if (DD2Helper.ShouldPlayDeathBlowVfx(creature))
        {
            if (shouldDie)
            {
                DD2Helper.PlayDeathVfx(creature, "DeathBlow");
            }
            else
            {
                if (preventer is DeathArmorPower && DD2Helper.ShouldPlayDeathArmorVfx(creature))
                {
                    await BroadcastDeathDoorEvent(creature, 0, 0, DeathDoorType.DeathArmor);
                    DD2Helper.PlayDeathVfx(creature, "DeathArmor");
                }
            }
        }
        DD2Helper.UnRegisterCreaturePosDoomed(creature);
    }

    public Task AfterDeathDoor(Creature creature, decimal healthDelta, decimal powerDelta, DeathDoorType type)
    {
        if (DD2Helper.IsDD2Monster(creature))
        {
            DD2MonsterHelper.ResetMonsterAdvancedConditions(null, creature);
        }
        if (!DD2Helper.ShouldPlayDeathDoorVfx(creature)) return Task.CompletedTask;
        //Jerry的苦修初始遗物是受到致命伤害改为获得灾厄，所以即使毒层数大于血量也无需提示死门
        if (DD2Helper.IsFlagellant(creature) && type == DeathDoorType.Poison) return Task.CompletedTask;

        if ((type == DeathDoorType.LowHealth && DD2ModConfigCfg.ShouldPlayDeathDoorVfxIfLowHealth)
            || (type == DeathDoorType.Doom && DD2ModConfigCfg.ShouldPlayDeathDoorVfxIfDoomed)
            || (type == DeathDoorType.Poison && DD2ModConfigCfg.ShouldPlayDeathDoorVfxIfPoisoned))
        {
            DD2Helper.PlayDeathVfx(creature, "DeathDoor");
        }
        return Task.CompletedTask;
    }

    private async Task BroadcastDeathDoorEvent(Creature creature, decimal healthDelta, decimal powerDelta, DeathDoorType type)
    {
        if (creature == null || creature.CombatState == null) return;

        foreach (AbstractModel item in creature.CombatState.IterateHookListeners())
        {
            if (item is IAfterDeathDoor myModel)
            {
                await myModel.AfterDeathDoor(creature, healthDelta, powerDelta, type);
            }
        }
    }
}