using GameServer.Battle.Core.Entities;
using GameServer.Battle.Core.Extensions;

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
            if (ToIndex < 0 || ToIndex >= deck.HandCards.Count) return false;

            var card = deck.HandCards[ToIndex];
            var config = context.CardCatalog.Get(card.ConfigId);
            int ownerId = config.OwnerId;

            context.AddActionPoint(SenderPlayerId, ownerId, 1);
            context.CheckAndAutoMerge(SenderPlayerId);

            return true;
        }

        public override void Undo(CombatContext context)
        {
            //TODO
        }
    }
}
