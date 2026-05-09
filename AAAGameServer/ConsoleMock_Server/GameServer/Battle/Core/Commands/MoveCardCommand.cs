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
            if (FromIndex == ToIndex) return true;

            var moveCard = deck.HandCards[FromIndex];
            var targetCard = deck.HandCards[ToIndex];
            var moveConfig = context.CardCatalog.Get(moveCard.ConfigId);
            var targetConfig = context.CardCatalog.Get(targetCard.ConfigId);
            if (moveConfig == null || targetConfig == null) return false;

            // 大招卡不参与交换
            if (moveConfig.CardType == CardType.Ultimate || targetConfig.CardType == CardType.Ultimate)
                return false;

            deck.HandCards.RemoveAt(FromIndex);
            deck.HandCards.Insert(ToIndex, moveCard);

            context.EventBus?.OnHandCardSwapped?.Invoke(SenderPlayerId, FromIndex, ToIndex);
            context.EventBus?.OnHandCardsUpdated?.Invoke(SenderPlayerId, new List<CardEntity>(deck.HandCards));

            while (context.CheckAndAutoMerge(SenderPlayerId)) { }

            return true;
        }

        public override void Undo(CombatContext context)
        {
            //TODO
        }
    }
}
