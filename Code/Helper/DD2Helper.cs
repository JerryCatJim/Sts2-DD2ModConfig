using DD2ModConfig.Code.Abstract;
using DD2ModConfig.Code.Audio;
using DD2ModConfig.Code.Config;
using DD2ModConfig.Code.Monster.DeathMonster;
using DD2ModConfig.Code.Powers;
using DD2ModConfig.Code.Relics;
using DD2ModConfig.Code.Singleton;
using DD2ModConfig.Code.VFX;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace DD2ModConfig.Code.Helper;

public static class DD2Helper
{
    public static bool IsDD2Character(Creature? creature)
    {
        return creature != null && creature.Player != null && creature.Player.Character is IGetDD2CharacterType DD2Character &&
            !string.IsNullOrEmpty(DD2Character.TryGetCharacterType());
    }
    public static bool IsDD2Character(Player? player)
    {
        return player != null && player.Character is IGetDD2CharacterType DD2Character &&
            !string.IsNullOrEmpty(DD2Character.TryGetCharacterType());
    }
    public static bool IsDD2Monster(Creature? creature)
    {
        return creature != null && creature.Monster is IGetDD2MonsterType DD2Monster &&
            !string.IsNullOrEmpty(DD2Monster.TryGetMonsterType());
    }
    public static string GetDD2MonsterName(Creature? creature)
    {
        if (creature != null && creature.Monster is IGetDD2MonsterType DD2Monster &&
                    !string.IsNullOrEmpty(DD2Monster.TryGetMonsterType()))
        {
            return DD2Monster.TryGetMonsterType();
        }
        return "";
    }
    public static bool IsFlagellant(Creature? creature)
    {
        return creature != null && creature.Player != null && creature.Player.Character is IGetDD2CharacterType DD2Character && DD2Character.TryGetCharacterType() == "Flagellant";
    }
    public static bool IsFlagellant(Player? player)
    {
        return player != null && player.Character is IGetDD2CharacterType DD2Character && DD2Character.TryGetCharacterType() == "Flagellant";
    }
    public static void ClearStressAfterCombatVictory(Player? player)
    {
        if (player == null) return;
        foreach(StressCounterRelic relic in player.Relics.Where(r => r is StressCounterRelic && r.Owner == player).ToList())
        {
            relic.CurrentStress = 0;
        }
    }
    public static bool IsInDeathDoor(Creature? creature)
    {
        return WillDieInPoison(creature) || WillDieInDoom(creature) || IsInDeathDoorHp(creature);
    }
    public static bool WillDieInDoom(Creature? creature, decimal hpDelta = 0m, decimal doomDelta = 0m)
    {
        decimal doomNum = (creature?.GetPower<DoomPower>()?.Amount ?? 0) - doomDelta;
        //delta均为0时获取的是当前的状态，减去delta获取的是上次状态
        return creature != null && doomNum > 0 && (doomNum >= creature.CurrentHp - hpDelta);
    }
    public static bool WillDieInPoison(Creature? creature, decimal hpDelta = 0m, decimal poisonDelta = 0m)
    {
        PoisonPower? poisonPower = creature?.GetPower<PoisonPower>();
        //delta均为0时获取的是当前的状态，减去delta获取的是上次状态
        return creature != null && poisonPower != null 
            && (PoisonPowerHelper.CalculateTotalDamageNextTurn(poisonPower, poisonPower.Amount - (int)poisonDelta) >= creature.CurrentHp - hpDelta);
    }
    public static bool IsInDeathDoorHp(Creature? creature, decimal hpDelta = 0m)
    {
        //delta均为0时获取的是当前的状态，减去delta获取的是上次状态
        return creature != null && (100m * (creature.CurrentHp - hpDelta) / creature.MaxHp) <= GetDeathDoorPercent(creature);
    }
    public static bool IsLowHealth(Creature? creature, decimal Percent = 30m)
    {
        if (creature == null) return false;

        return 100m * creature.CurrentHp / creature.MaxHp <= Percent;
    }

    public static bool IsStressGreaterEqual(Creature? creature, decimal num = 5m)
    {
        if (creature == null) return false;

        return creature.GetPower<StressPower>() is StressPower stressPower && stressPower.Amount >= num;
    }

    public static bool IsStressLessEqual(Creature? creature, decimal num = 5m)
    {
        if (creature == null) return false;

        StressPower? stressPower = creature.GetPower<StressPower>();
        return stressPower == null || (stressPower != null && stressPower.Amount <= num);
    }

    public static void ResetAllValues()
    {
        DD2CombatSingleton.ResetAllValue();
        DeathListenForRunStateSingleton.ResetValue();
        DD2AudioManager.StopDD2Bgm();
        ResetCombatDictionaries();
    }

    public static decimal GetDeathDoorPercent(Creature creature)
    {
        if (creature == null) return 0m;

        if (creature.IsPlayer)
        {
            return (decimal)DD2ModConfigCfg.PlayerShowDeathDoorVfxHpPercent;
        }
        else
        {
            return (decimal)DD2ModConfigCfg.MonsterShowDeathDoorVfxHpPercent;
        }
    }

    public static void ResetCombatDictionaries()
    {
        _creaturesPosDoomed.Clear();
        _activeDeathVfx.Clear();
        _lastVfxName.Clear();
    }

    private static readonly Dictionary<Creature, Vector2> _creaturesPosDoomed = new();
    public static bool RegisterCreaturePosDoomed(Creature creature, Vector2 pos)
    {
        if (creature == null) return false;

        _creaturesPosDoomed[creature] = pos;
        return true;
    }
    public static void UnRegisterCreaturePosDoomed(Creature creature)
    {
        if (creature == null) return;
        _creaturesPosDoomed.Remove(creature);
    }

    private static readonly Dictionary<Creature, Node2D> _activeDeathVfx = new();
    private static readonly Dictionary<Creature, string> _lastVfxName = new();
    public static void PlayDeathVfx(Creature creature, string DeathBlowName)
    {
        if (creature == null) return;

        bool shouldPlaySfx = true;
        if (_activeDeathVfx.TryGetValue(creature, out Node2D? vfx) && vfx != null)
        {
            //如果还在播放上一个特效时被打死，强制刷新播放致命一击特效
            if (DeathBlowName == "DeathBlow")
            {
                _activeDeathVfx.Remove(creature);
                _lastVfxName.Remove(creature);
                vfx.QueueFreeSafely();
            }
            else if (DeathBlowName == "DeathArmor")
            {
                //连续击破死亡护甲时仅刷新动画不重放声音
                shouldPlaySfx = !_lastVfxName.TryGetValue(creature, out string? name) || name != "DeathArmor";

                _activeDeathVfx.Remove(creature);
                _lastVfxName.Remove(creature);
                vfx.QueueFreeSafely();
            }
            else //DeathDoor则不刷新
            {
                return;
            }
        }

        if (shouldPlaySfx)
        {
            PlayDeathSfx(creature, DeathBlowName);
        }

        string NodeName = DeathBlowName;
        Node2D vfxNode = VFXUtil.GetVFXNode("res://DD2ModConfig/Scenes/DD2Scenes/" + NodeName + ".tscn");
        //Node2D vfxNode = PreloadManager.Cache.GetScene("res://DD2ModConfig/Scenes/DD2Scenes/" + NodeName + ".tscn").Instantiate<Node2D>();
        if (vfxNode == null)
        {
            Log.Error("[DD2Helper PlayDeathVfx]: " + NodeName + ".tscn load failed.");
            return;
        }

        if (NCombatRoom.Instance != null && NCombatRoom.Instance.CombatVfxContainer != null)
        {
            Vector2 globalPos;
            Vector2 visualScale;

            NCreature? nCreature = creature.GetCreatureNode();
            if (nCreature == null || !GodotObject.IsInstanceValid(nCreature) ||
                nCreature.Visuals == null || !GodotObject.IsInstanceValid(nCreature.Visuals))
            {
                _creaturesPosDoomed.TryGetValue(creature, out globalPos);
                visualScale = new Vector2(1, 1);
            }
            else
            {
                globalPos = nCreature.Visuals.GetNodeOrNull<Marker2D>("%CenterPos")?.GlobalPosition ?? nCreature.GlobalPosition;
                visualScale = nCreature.Visuals.Scale;
            }
            if (vfxNode.GetParent() == null)
            {
                NCombatRoom.Instance.CombatVfxContainer.AddChild(vfxNode);
            }
            vfxNode.GlobalPosition = globalPos;
            vfxNode.Scale = visualScale;
        }
        else
        {
            vfxNode.QueueFreeSafely();
        }

        var AnimPlayer = vfxNode.GetNodeOrNull<AnimationPlayer>(NodeName + "AnimPlayer");
        if (AnimPlayer != null)
        {
            _activeDeathVfx[creature] = vfxNode;
            _lastVfxName[creature] = DeathBlowName;
            AnimPlayer.AnimationFinished += (StringName state) =>
            {
                if (_activeDeathVfx.TryGetValue(creature, out var currentVfx) && currentVfx == vfxNode)
                {
                    _activeDeathVfx.Remove(creature);
                    _lastVfxName.Remove(creature);
                }
                vfxNode.QueueFreeSafely();
            };
            AnimPlayer.Stop();
            AnimPlayer.Play("Show");
        }
        else
        {
            vfxNode.QueueFreeSafely();
        }
    }
    public static void PlayDeathSfx(Creature creature, string DeathBlowName)
    {
        if (DeathBlowName == "DeathBlow")
        {
            DD2AudioManager.PlayDD2Sfx(IsFlagellant(creature) ? "DeathBlowDoom" : "DeathBlow", false, false, -2);
        }
        else if (DeathBlowName == "DeathArmor")
        {
            DD2AudioManager.PlayDD2Sfx("DeathArmor", false, false, -4);
        }
        else if (DeathBlowName == "DeathDoor")
        {
            DD2AudioManager.PlayDD2Sfx("DeathDoor", false, false, -4);
        }
    }

    public static void ReparentActiveVfxNodes()
    {
        foreach (KeyValuePair<Creature, Node2D> pairs in _activeDeathVfx)
        {
            if (pairs.Value != null)
            {
                if (NGame.Instance != null && NGame.Instance.RootSceneContainer != null)
                {
                    pairs.Value.Reparent(NGame.Instance.RootSceneContainer);
                }
            }
        }
    }

    public static bool ShouldPlayDeathDoorVfx(Creature creature)
    {
        if (creature == null) return false;

        return (creature.IsPlayer && DD2ModConfigCfg.ShouldPlayerShowDeathDoorVfx) ||
            (!creature.IsPlayer && DD2ModConfigCfg.ShouldMonsterShowDeathDoorVfx);
    }
    public static bool ShouldPlayDeathArmorVfx(Creature creature)
    {
        if (creature == null) return false;

        return (creature.IsPlayer && DD2ModConfigCfg.ShouldPlayerShowDeathArmorVfx) ||
            (!creature.IsPlayer && DD2ModConfigCfg.ShouldMonsterShowDeathArmorVfx);
    }
    public static bool ShouldPlayDeathBlowVfx(Creature creature)
    {
        if (creature == null) return false;

        return (creature.IsPlayer && DD2ModConfigCfg.ShouldPlayerShowDeathBlowVfx) ||
            (!creature.IsPlayer && DD2ModConfigCfg.ShouldMonsterShowDeathBlowVfx);
    }
}
