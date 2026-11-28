using DD2ModConfig.Code.Audio;
using DD2ModConfig.Code.Config;
using DD2ModConfig.Code.Monster.DeathMonster;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
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
    public static void ResetValue()
    {
        //战斗结束后会在DeathListenForCombatStateSingleton里调用此函数
        //但不用清除_deathSpawnVfxInstance，因为击败死神后会手动调用ClearDeathSpawnVfxAndBgm
        TweensList.Clear();
        FocusedNCreatureSet.Clear();
        _isFocusing = false;
    }

    public static readonly List<Tween> TweensList = new List<Tween>();
    public static readonly HashSet<NCreature> FocusedNCreatureSet = new HashSet<NCreature>();
    private static Node2D? _deathSpawnVfxInstance;
    public static void PlayDeathSpawnVfx(Creature creature)
    {
        if (GodotObject.IsInstanceValid(_deathSpawnVfxInstance) && !_deathSpawnVfxInstance.IsQueuedForDeletion())
        {
            _deathSpawnVfxInstance.QueueFreeSafely();
            _deathSpawnVfxInstance = null;
        }
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

            NCreature? nCreature = NCombatRoom.Instance?.GetCreatureNode(creature);
            if (nCreature != null)
            {
                FocusAndReturn(nCreature, new Vector2(1080, 420));
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

    private static bool _isFocusing = false;
    public static void FocusAndReturn(NCreature MonsterCreature, Vector2 targetLocal, float targetScale = 1.25f,
        float zoomInDuration = 3.0f, float waitingDuration = 4.5f, float zoomOutDuration = 1.3f)
    {
        Control? layer = NCombatRoom.Instance?.SceneContainer;
        if (!GodotObject.IsInstanceValid(layer) || !layer.IsInsideTree())
        {
            return;
        }

        _isFocusing = true;
        // 1. 先解除震动
        NGame.Instance?.ClearScreenShakeTarget();

        // 保险起见，延迟一帧读取初始状态，确保 ClearScreenShakeTarget 产生的副作用已结算
        Callable.From(() =>
        {
            if (!_isFocusing) return;

            if (GodotObject.IsInstanceValid(MonsterCreature))
            {
                FocusedNCreatureSet.Add(MonsterCreature);
                //不可交互，隐藏血条和意图
                MonsterCreature.SetIsInteractable(false, true);
                MonsterCreature.IntentContainer.Visible = false;
            }

            if (!GodotObject.IsInstanceValid(layer)) return;

            Vector2 originalScale = DeathListenForCombatStateSingleton.CameraOriginalScale;//layer.Scale;
            Vector2 originalPos = DeathListenForCombatStateSingleton.CameraOriginalPos;//layer.Position;
            Vector2 screenCenter = layer.Size / 2f;
            Vector2 targetPos = originalPos + screenCenter - (targetLocal * targetScale);

            // 第一次 Tween：拉近聚焦
            var zoomInTween = layer.CreateTween().SetParallel(true);
            TweensList.Add(zoomInTween);
            zoomInTween.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            zoomInTween.TweenProperty(layer, "scale", originalScale * targetScale, zoomInDuration);
            zoomInTween.TweenProperty(layer, "position", targetPos, zoomInDuration);

            zoomInTween.Finished += () =>
            {
                TweensList.Remove(zoomInTween);
                if (!GodotObject.IsInstanceValid(layer)) return;

                var waitingTween = layer.CreateTween().SetParallel(true);
                TweensList.Add(waitingTween);
                waitingTween.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
                waitingTween.TweenProperty(layer, "scale", originalScale * targetScale * 1.1f, waitingDuration);
                waitingTween.TweenProperty(layer, "position", targetPos + (new Vector2(-60, 60) * originalScale), waitingDuration);
                // 等待时间
                waitingTween.Finished += () =>
                {
                    TweensList.Remove(waitingTween);
                    if (!GodotObject.IsInstanceValid(layer)) return;

                    if (GodotObject.IsInstanceValid(MonsterCreature))
                    {
                        //提前恢复血条，血条恢复动画有一段淡入淡出的延迟
                        MonsterCreature.AnimateInStateDisplayForCombatStart();
                    }

                    var zoomOutTween = layer.CreateTween().SetParallel(true);
                    TweensList.Add(zoomOutTween);
                    zoomOutTween.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
                    zoomOutTween.TweenProperty(layer, "scale", originalScale, zoomOutDuration);
                    zoomOutTween.TweenProperty(layer, "position", originalPos, zoomOutDuration);

                    // 监听复位结束
                    zoomOutTween.Finished += () =>
                    {
                        TweensList.Remove(zoomOutTween);

                        _isFocusing = false;

                        if (GodotObject.IsInstanceValid(MonsterCreature))
                        {
                            //恢复可交互和意图
                            MonsterCreature.SetIsInteractable(true, false);
                            MonsterCreature.IntentContainer.Visible = true;
                        }

                        if (!GodotObject.IsInstanceValid(layer)) return;

                        // 关键修复 1：强制精准复位（消除 Tween 产生的浮点数微小误差）
                        // 防止 Control 的布局系统发现坐标不对而强行吸附
                        layer.Scale = originalScale;
                        layer.Position = originalPos;

                        // 关键修复 2：延迟一帧再恢复屏幕震动
                        // 防止震动系统在 Tween 结束的同一帧强行抢夺 Position，导致画面撕裂跳动
                        Callable.From(() =>
                        {
                            if (GodotObject.IsInstanceValid(layer))
                            {
                                NGame.Instance?.SetScreenShakeTarget(layer);
                            }
                        }).CallDeferred();
                    };
                };
            };
        }).CallDeferred();
    }
    public static void ClearAllTweens(bool immediately = false, float zoomOutDuration = 0.8f)
    {
        _isFocusing = false;

        foreach (Tween? tween in TweensList)
        {
            if (GodotObject.IsInstanceValid(tween))
            {
                tween.Kill();
            }
        }
        TweensList.Clear();

        Control? layer = NCombatRoom.Instance?.SceneContainer;
        if (!GodotObject.IsInstanceValid(layer) || !layer.IsInsideTree())
        {
            return;
        }

        Vector2 originalScale = DeathListenForCombatStateSingleton.CameraOriginalScale;
        Vector2 originalPos = DeathListenForCombatStateSingleton.CameraOriginalPos;

        foreach (NCreature? nCreature in FocusedNCreatureSet)
        {
            //恢复所有被聚焦中的怪物的可交互和意图，注意第二个参数填入true立刻恢复血条
            if (GodotObject.IsInstanceValid(nCreature))
            {
                nCreature.SetIsInteractable(true, true);
                nCreature.IntentContainer.Visible = true;
            }
        }
        FocusedNCreatureSet.Clear();

        void RestoreScreenShake()
        {
            Callable.From(() =>
            {
                if (GodotObject.IsInstanceValid(layer))
                {
                    NGame.Instance?.SetScreenShakeTarget(layer);
                }
            }).CallDeferred();
        }

        //完全一致则什么也不做
        if (layer.Position == originalPos && layer.Scale == originalScale)
        {
            RestoreScreenShake();
            return;
        }

        if (immediately)
        {
            layer.Scale = originalScale;
            layer.Position = originalPos;
            RestoreScreenShake();

        }
        else  //套了一层镜头Tween
        {
            var zoomOutTween = layer.CreateTween().SetParallel(true);
            TweensList.Add(zoomOutTween);
            zoomOutTween.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            zoomOutTween.TweenProperty(layer, "scale", originalScale, zoomOutDuration);
            zoomOutTween.TweenProperty(layer, "position", originalPos, zoomOutDuration);

            // 监听复位结束
            zoomOutTween.Finished += () =>
            {
                if (GodotObject.IsInstanceValid(layer))
                {
                    layer.Scale = originalScale;
                    layer.Position = originalPos;
                    RestoreScreenShake();
                }
                TweensList.Remove(zoomOutTween);
            };
        }
    }
}
