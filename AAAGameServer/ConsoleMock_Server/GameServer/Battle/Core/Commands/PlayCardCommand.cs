/*
* ┌──────────────────────────────────┐
* │  描    述: 出牌命令，负责从手牌打出卡牌并处理行动点
* │  类    名: PlayCardCommand.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using GameServer.Battle.Core.Entities;
using GameServer.Battle.Core.Extensions;
using GameServer.Battle.Data;
using GameServer.Battle.Data.Config;

namespace GameServer.Battle.Core.Commands
{
    internal class PlayCardCommand : BaseCommand
    {
        public CardEntity Card { get; }
        public int TargetInstanceId { get; }
        public int OriginalHandIndex { get; }

        public PlayCardCommand(int senderPlayerId, CardEntity card, int targetInstanceId, int originalHandIndex) : base(senderPlayerId)
        {
            Card = card;
            TargetInstanceId = targetInstanceId;
            OriginalHandIndex = originalHandIndex;
        }

        public override bool Execute(CombatContext context)
        {
            if (!context.PlayerDecks.TryGetValue(SenderPlayerId, out var deck)) return false;

            // 从手牌移除
            int removeIndex = deck.HandCards.FindIndex(c => c.InstanceId == Card.InstanceId);
            if (removeIndex == -1) return false;
            deck.HandCards.RemoveAt(removeIndex);

            // 加入行动队列
            context.ActionQueue.QueuedCards.Add(Card);

            // 弃置（普通牌进弃牌堆，大招牌销毁）
            var config = context.CardCatalog.Get(Card.ConfigId);
            if (config.CardType != CardType.Ultimate)
            {
                Card.StarLevel = 1;
                deck.DiscardPile.Add(Card);
                context.EventBus?.OnCardDiscarded?.Invoke(SenderPlayerId, Card);
            }

            // 行动点处理
            if (config.CardType == CardType.Ultimate)
            {
                context.ClearActionPoint(SenderPlayerId, config.OwnerId);
            }
            else
            {
                context.AddActionPoint(SenderPlayerId, config.OwnerId, 1);
            }

            context.EventBus?.OnCardPlayed?.Invoke(SenderPlayerId, Card, TargetInstanceId);
            context.EventBus?.OnHandCardsUpdated?.Invoke(SenderPlayerId, new List<CardEntity>(deck.HandCards));

            // 自动检查合成
            context.CheckAndAutoMerge(SenderPlayerId);

            return true;
        }

        public override void Undo(CombatContext ctx)
        {
            //TODO
        }
    }
}
