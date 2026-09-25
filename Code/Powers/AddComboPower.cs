using DD2ModConfig.Code.Abstract;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace DD2ModConfig.Code.Powers;

public sealed class AddComboPower : DD2ModConfigPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ComboPower>()
    ];

    private class Data
    {
        public readonly Dictionary<CardModel, List<Creature>> playedCards = new Dictionary<CardModel, List<Creature>>();
    }

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != base.Owner || cardPlay.Card.Type != CardType.Attack)
        {
            return Task.CompletedTask;
        }
        GetInternalData<Data>().playedCards.TryAdd(cardPlay.Card, new List<Creature>());
        return Task.CompletedTask;
    }

    public override Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (CombatManager.Instance.IsInProgress && dealer == base.Owner && target != base.Owner
            && result.TotalDamage > 0
            && cardSource != null && cardSource.Owner.Creature == base.Owner
            && cardSource.Type == CardType.Attack)
        {
            if (GetInternalData<Data>().playedCards.TryGetValue(cardSource, out List<Creature>? value)
                && value is List<Creature> myList && !myList.Exists((Creature c) => c == target))
            {
                myList.Add(target);
            }
        }
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay == null) return;

        if (GetInternalData<Data>().playedCards.Remove(cardPlay.Card, out List<Creature>? CreatureList) && CreatureList != null)
        {
            Flash();
            if (CreatureList.Count <= 0)
            {
                if (cardPlay.Card.Type == CardType.Attack && cardPlay.Target != null && cardPlay.Target.IsAlive)
                {
                    await PowerCmd.Apply<ComboPower>(context, cardPlay.Target, 1, Owner, cardPlay.Card);
                }
            }
            else
            {
                foreach (Creature? enemy in CreatureList)
                {
                    if (enemy != null && enemy.IsAlive)
                    {
                        await PowerCmd.Apply<ComboPower>(context, enemy, 1, Owner, cardPlay.Card);
                    }
                }
            }
            await PowerCmd.Decrement(this);
        }
    }
}
