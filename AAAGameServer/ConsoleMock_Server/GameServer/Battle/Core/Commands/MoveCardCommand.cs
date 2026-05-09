using GameServer.Battle.Core.Entities;
using GameServer.Battle.Core.Extensions;
using GameServer.Battle.Data.Config;

namespace GameServer.Battle.Core.Commands
{
    internal class MoveCardCommand : BaseCommand
    {
        public int FromIndex { get; }
        public int ToIndex { get; }

        public MoveCardCommand(int senderPlayerId, int fromIndex, int toIndex) : base(senderPlayerId)
        {
            FromIndex = fromIndex;
            ToIndex = toIndex;
        }

        public override bool Execute(CombatContext context)
        {
            var deck = context.PlayerDecks[SenderPlayerId];
            if (FromIndex < 0 || FromIndex >= deck.HandCards.Count) return false;
            if (ToIndex < 0 || ToIndex >= deck.HandCards.Count) return false;

            var cardA = deck.HandCards[FromIndex];
            var cardB = deck.HandCards[ToIndex];
            var configA = context.CardCatalog.Get(cardA.ConfigId);
            var configB = context.CardCatalog.Get(cardB.ConfigId);
            if (configA == null || configB == null) return false;

            // 大招卡不参与交换
            if (configA.CardType == CardType.Ultimate || configB.CardType == CardType.Ultimate)
                return false;

            (deck.HandCards[FromIndex], deck.HandCards[ToIndex]) = (deck.HandCards[ToIndex], deck.HandCards[FromIndex]);

            context.EventBus?.OnHandCardSwapped?.Invoke(SenderPlayerId, FromIndex, ToIndex);
            context.EventBus?.OnHandCardsUpdated?.Invoke(SenderPlayerId, new List<CardEntity>(deck.HandCards));

            return true;
        }

        public override void Undo(CombatContext context)
        {
            //TODO
        }
    }
}
