using DD2ModConfig.Code.Audio;
using DD2ModConfig.Code.Config;
using DD2ModConfig.Code.Monster.DeathMonster;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace DD2ModConfig.Code.Helper;

public static class DD2MonsterHelper
{
    public static void ResetMonsterAdvancedConditions(AnimationTree? animTree, Creature creature)
    {
        if (creature == null) return;

        AnimationTree? animationTree = animTree;
        if (animationTree == null)
        {
            NCreature? creatureNode = creature.GetCreatureNode();
            if (creatureNode != null)
            {
                animationTree = creatureNode.Visuals.GetNodeOrNull<AnimationTree>("AnimationTree");
            }
        }
        if (animationTree != null)
        {
            bool toDeathIdle = DD2Helper.IsInDeathDoor(creature) || creature.CurrentHp == 1m;
            animationTree.Set("parameters/Idle/conditions/ToIdle", !toDeathIdle);
            animationTree.Set("parameters/Idle/conditions/ToDeathIdle", toDeathIdle);
        }
    }

    private static Node2D? _deathSpawnVfxInstance;
    public static void PlayDeathSpawnVfx()
    {
        //若多个死神同时出现只加载一次滤镜
        if (DeathListenForCombatStateSingleton.HasDeathSpawnVfx == false && DD2ModConfigCfg.ShouldShowDeathEncounterVfx)
        {
            _deathSpawnVfxInstance = PreloadManager.Cache.GetScene("res://DD2ModConfig_Asset/Monster_Death/EnterEffect/EnterEffect.tscn").Instantiate<Node2D>();
            if (_deathSpawnVfxInstance == null) return;

            _deathSpawnVfxInstance.Position = Vector2.Zero;
            _deathSpawnVfxInstance.Scale = Vector2.One;

            var animPlayer = _deathSpawnVfxInstance.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
            if (animPlayer != null)
            {
                animPlayer.Stop();
                animPlayer.Play("Show");
            }

            var combatRoom = NCombatRoom.Instance;
            if (combatRoom != null)
            {
                if (_deathSpawnVfxInstance.GetParent() == null)
                {
                    combatRoom.AddChild(_deathSpawnVfxInstance);
                }
            }
            DeathListenForCombatStateSingleton.HasDeathSpawnVfx = true;
        }
    }
    public static void ClearDeathSpawnVfxAndBgm(bool immediately = false)
    {
        DD2AudioManager.StopDD2Bgm();

        Node2D? targetInstance = _deathSpawnVfxInstance;
        _deathSpawnVfxInstance = null; // 立刻将全局变量清空，释放控制权

        if (!GodotObject.IsInstanceValid(targetInstance) || targetInstance.IsQueuedForDeletion())
        {
            return;
        }

        if (immediately)
        {
            targetInstance.QueueFreeSafely();
            return;
        }

        var animPlayer = targetInstance.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        if (animPlayer == null)
        {
            targetInstance.QueueFreeSafely();
            return;
        }

        animPlayer.Stop();
        animPlayer.AnimationFinished += (StringName animName) =>
        {
            if (animName == "End")
            {
                //闭包一定要捕获函数里的临时变量，而不是全局变量_deathSpawnVfxInstance
                targetInstance.QueueFreeSafely();
            }
        };
        animPlayer.Play("End");

        //动画播放超时等情况，用于兜底清理
        var tree = targetInstance.GetTree();
        if (tree != null)
        {
            SceneTreeTimer timer = tree.CreateTimer(5.0f);
            timer.Timeout += () =>
            {
                if (GodotObject.IsInstanceValid(targetInstance) && !targetInstance.IsQueuedForDeletion())
                {
                    targetInstance.QueueFreeSafely();
                }
            };
        }
    }
}
