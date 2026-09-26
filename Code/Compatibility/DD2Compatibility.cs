using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace DD2ModConfig.Code.Compatibility;

public static class DD2Compatibility
{
    private static DD2VariableMethod _modifyDamage = new DD2VariableMethod(
    (typeof(Hook), "ModifyDamage", new Type[11]
    {
        typeof(IRunState),
        typeof(ICombatState),
        typeof(Creature),
        typeof(Creature),
        typeof(decimal),
        typeof(ValueProp),
        typeof(CardModel),
        typeof(CardPlay),
        typeof(ModifyDamageHookType),
        typeof(CardPreviewMode),
        typeof(IEnumerable<AbstractModel>).MakeByRefType()
    }, new int[11] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }),
    (typeof(Hook), "ModifyDamage", new Type[10]
    {
        typeof(IRunState),
        typeof(ICombatState),
        typeof(Creature),
        typeof(Creature),
        typeof(decimal),
        typeof(ValueProp),
        typeof(CardModel),
        typeof(ModifyDamageHookType),
        typeof(CardPreviewMode),
        typeof(IEnumerable<AbstractModel>).MakeByRefType()
    }, new int[10] { 0, 1, 2, 3, 4, 5, 6, 8, 9, 10 }));

    public static decimal ModifyDamageCompatibility(IRunState runState, ICombatState? combatState, Creature? target, Creature? dealer, decimal damage, ValueProp props, CardModel? cardSource, CardPlay? cardPlay, ModifyDamageHookType modifyDamageHookType, CardPreviewMode previewMode, out IEnumerable<AbstractModel> modifiers)
    {
        //静态类的第一个参数this调用者传入null
        (decimal modifiedDamage, object?[] Args) pairs = _modifyDamage.InvokeWithArgs<decimal>(null, new object?[11] { runState, combatState, target, dealer, damage, props, cardSource, cardPlay, modifyDamageHookType, previewMode, null});
        modifiers = (IEnumerable<AbstractModel>?)pairs.Args[^1] ?? Enumerable.Empty<AbstractModel>();
        return pairs.modifiedDamage;
    }
}
