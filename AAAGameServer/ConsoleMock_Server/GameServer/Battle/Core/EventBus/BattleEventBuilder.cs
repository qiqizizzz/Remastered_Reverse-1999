/*
* ┌──────────────────────────────────┐
* │  描    述: 战斗事件构建器，将 CombatEventBus 事件转为 Proto BattleEvent
* │  类    名: BattleEventBuilder.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using GameProtocol;
using GameServer.Battle.Core.Entities;

namespace GameServer.Battle.Core.EventBus
{
    internal class BattleEventBuilder
    {
        private readonly CombatContext _context;
        private readonly List<BattleEvent> _events;

        public BattleEventBuilder(CombatContext context)
        {
            _context = context;
            _events = new List<BattleEvent>();
            subscribeEvents(context.EventBus);
        }

        // 收集已构建的事件并清空缓存
        public List<BattleEvent> CollectAndClear()
        {
            var result = new List<BattleEvent>(_events);
            _events.Clear();
            return result;
        }

        // 手动添加事件（用于 BattleStart / TurnStart / TurnEnd / BattleEnd 等）
        public void AddEvent(BattleEvent evt)
        {
            _events.Add(evt);
        }

        // ==================== 订阅 CombatEventBus 事件 ====================
        private void subscribeEvents(CombatEventBus eventBus)
        {
            eventBus.OnDamageTaken += onDamageTaken;
            eventBus.OnHealTaken += onHealTaken;
            eventBus.OnEntityDied += onEntityDied;
            eventBus.OnActionPointChanged += onActionPointChanged;
            eventBus.OnCardDrawn += onCardDrawn;
            eventBus.OnCardDiscarded += onCardDiscarded;
            eventBus.OnCardMerged += onCardMerged;
            eventBus.OnHandCardSwapped += onHandCardSwapped;
            eventBus.OnCardPlayed += onCardPlayed;
            eventBus.OnUltimateCardGranted += onUltimateCardGranted;
            eventBus.OnDeckShuffled += onDeckShuffled;
        }

        // ==================== 实体状态事件 ====================
        // 受到伤害
        private void onDamageTaken(int targetId, int damage, bool isCrit)
        {
            _events.Add(new BattleEvent
            {
                EventType = BattleEventType.DamageTaken,
                TargetId = targetId,
                Damage = new DamageParams { DamageValue = damage, IsCritical = isCrit }
            });
        }

        // 受到治疗
        private void onHealTaken(int targetId, int heal)
        {
            _events.Add(new BattleEvent
            {
                EventType = BattleEventType.HealTaken,
                TargetId = targetId,
                Heal = new HealParams { HealValue = heal, IsOverHeal = false }
            });
        }

        // 实体死亡
        private void onEntityDied(int instanceId)
        {
            _events.Add(new BattleEvent
            {
                EventType = BattleEventType.EntityDied,
                TargetId = instanceId,
                Death = new DeathParams()
            });
        }

        // 行动点变化
        private void onActionPointChanged(int playerId, int ownerId, int currentValue)
        {
            _events.Add(new BattleEvent
            {
                EventType = BattleEventType.ActionPointChanged,
                ActionPointChanged = new ActionPointChangedParams
                {
                    EntityId = ownerId,
                    NewValue = currentValue,
                    MaxValue = 5
                }
            });
        }

        // ==================== 牌库与手牌事件 ====================
        // 抽牌
        private void onCardDrawn(int playerId, CardEntity card)
        {
            _events.Add(new BattleEvent
            {
                EventType = BattleEventType.DrawCard,
                DrawCard = new DrawCardParams
                {
                    Card = toProtoCard(card),
                    SlotIndex = 0
                }
            });
        }

        // 弃牌
        private void onCardDiscarded(int playerId, CardEntity card)
        {
            _events.Add(new BattleEvent
            {
                EventType = BattleEventType.DiscardCard,
                DiscardCard = new DiscardCardParams
                {
                    Card = toProtoCard(card),
                    SlotIndex = 0
                }
            });
        }

        // 合成卡牌
        private void onCardMerged(int playerId, CardEntity kept, CardEntity destroyed, int newStar)
        {
            _events.Add(new BattleEvent
            {
                EventType = BattleEventType.MergeCard,
                MergeCard = new MergeCardParams
                {
                    ResultCardInstanceId = kept.InstanceId,
                    ResultStarLevel = newStar,
                    ConsumedCardIds = { destroyed.InstanceId },
                    SlotIndex = 0
                }
            });
        }

        // 手牌交换
        private void onHandCardSwapped(int playerId, int fromIndex, int toIndex)
        {
            _events.Add(new BattleEvent
            {
                EventType = BattleEventType.CardMoved,
                MoveCard = new MoveCardParams
                {
                    FromIndex = fromIndex,
                    ToIndex = toIndex
                }
            });
        }

        // 卡牌进入行动队列
        private void onCardPlayed(int playerId, CardEntity card, int targetInstanceId)
        {
            int queueIndex = _context.ActionQueue.QueuedCards.Count - 1;
            int actionPoint = 0;
            if (_context.Entities.TryGetValue(card.ConfigId, out var entity))
                actionPoint = entity.ActionPoint;

            _events.Add(new BattleEvent
            {
                EventType = BattleEventType.EnqueueCard,
                TargetId = targetInstanceId,
                EnqueueCard = new EnqueueCardParams
                {
                    Card = toProtoCard(card),
                    QueueIndex = queueIndex < 0 ? 0 : queueIndex,
                    ActionPointAfter = actionPoint
                }
            });
        }

        // 发放大招牌
        private void onUltimateCardGranted(int playerId, int characterConfigId, CardEntity ultimateCard)
        {
            _events.Add(new BattleEvent
            {
                EventType = BattleEventType.GrantUltimate,
                GrantUltimate = new GrantUltimateParams
                {
                    Card = toProtoCard(ultimateCard),
                    SlotIndex = 0
                }
            });
        }

        // 洗牌
        private void onDeckShuffled(int playerId)
        {
            _events.Add(new BattleEvent
            {
                EventType = BattleEventType.ShuffleDeck,
                ShuffleDeck = new ShuffleDeckParams { DeckOwnerId = playerId }
            });
        }

        // ==================== 工具函数 ====================
        // 将内部 CardEntity 转为 Proto CardEntityInfo
        private static CardEntityInfo toProtoCard(CardEntity card)
        {
            return new CardEntityInfo
            {
                InstanceId = card.InstanceId,
                ConfigId = card.ConfigId,
                StarLevel = card.StarLevel
            };
        }
    }
}
