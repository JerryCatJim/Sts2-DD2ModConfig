using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;

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
}
