using DD2ModConfig.Code.ResoluteOrMeltdown;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace DD2ModConfig.Code.Abstract;
public interface IAfterStressChanged
{
    public Task AfterStressAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource);
}
/*public interface IModifyHpAmountReceived
{
    public bool TryModifyHpAmountReceived(Creature creature, decimal amount, out decimal modifiedAmount, bool silent)
    {
        modifiedAmount = amount;
        return false;
    }
}*/
public interface IModifyResoluteprobability
{
    //百分制
    public bool TryModifyResoluteprobability(Creature creature, decimal amount, out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        return false;  //还没想好返回值有啥用
    }
}
public interface IAfterComboChanged
{
    public Task AfterComboChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature applier, CardModel? cardSource);
}

public enum DeathDoorType
{
    Doom,
    Poison,
    LowHealth,
    DeathArmor
}
public interface IBeforePoisonTrigger
{
    public void BeforePoisonTrigger(PowerModel power, decimal amount);
}

public interface IAfterDeathDoor
{
    public Task AfterDeathDoor(Creature creature, decimal healthDelta, decimal powerDelta, DeathDoorType Type);
}

public interface IBeforeResoluteOrMeltdownChanged
{
    public Task BeforeResoluteOrMeltdownChanged(PlayerChoiceContext choiceContext, Player player, ResoluteOrMeltdownModel oldRM, ResoluteOrMeltdownModel newRM);
}
public interface IOnResoluteOrMeltdownChanged
{
    public Task OnResoluteOrMeltdownChanged(PlayerChoiceContext choiceContext, Player player, ResoluteOrMeltdownModel oldRM, ResoluteOrMeltdownModel newRM);
}
public interface IAfterResoluteOrMeltdownChanged
{
    public Task AfterResoluteOrMeltdownChanged(PlayerChoiceContext choiceContext, Player player, ResoluteOrMeltdownModel oldRM, ResoluteOrMeltdownModel newRM);
}

public interface IModifyHpPercentEnterToxicAdditional
{
    public bool TryModifyHpPercentEnterToxicAdditional(Creature creature, decimal amount, out decimal modifiedAmount, bool silent)
    {
        modifiedAmount = amount;
        return false;  //是否打断后续其他相同接口的修改
    }
}
public interface IGetDD2CharacterType
{
    public string TryGetCharacterType();
}
public interface IGetDD2MonsterType
{
    public string TryGetMonsterType();
}