/*
* ┌──────────────────────────────────┐
* │  描    述: 战斗表现事件播放器，按顺序播放服务端推送的事件流
* │  类    名: PlayEventSequence.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Defines;
using GameProtocol;
using Module.Character;
using Module.fight.CardMgr;
using Module.fight.Core.Entities;
using UnityEngine;

namespace Module.fight
{
    public static class PlayEventSequence
    {
        private static bool _isPlaying;
        private static readonly Queue<IList<BattleEvent>> _eventQueue = new Queue<IList<BattleEvent>>();

        // 播放事件序列
        public static void Play(IList<BattleEvent> events)
        {
            if (events == null || events.Count == 0) return;

            _eventQueue.Enqueue(events);

            if (!_isPlaying)
                processQueue();
        }

        // 顺序处理队列中的事件序列
        private static async void processQueue()
        {
            _isPlaying = true;

            try
            {
                while (_eventQueue.Count > 0)
                {
                    var events = _eventQueue.Dequeue();
                    for (int i = 0; i < events.Count; i++)
                    {
                        var evt = events[i];
                        if (evt.EventType == BattleEventType.DamageTaken)
                        {
                            var batch = new List<BattleEvent> { evt };
                            while (i + 1 < events.Count && events[i + 1].EventType == BattleEventType.DamageTaken)
                            {
                                batch.Add(events[++i]);
                            }
                            await playDamageTakenBatch(batch);
                        }
                        else
                        {
                            await playSingleEvent(evt);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogError($"[PlayEventSequence] 播放事件序列异常: {ex}");
#endif
            }
            finally
            {
                _isPlaying = false;
            }
        }

        // 播放单个事件
        private static async Task playSingleEvent(BattleEvent evt)
        {
            switch (evt.EventType)
            {
                case BattleEventType.BattleStart:
                    await playBattleStart(evt);
                    break;
                case BattleEventType.BattleEnd:
                    await playBattleEnd(evt);
                    break;
                case BattleEventType.TurnStart:
                    await playTurnStart(evt);
                    break;
                case BattleEventType.TurnEnd:
                    await playTurnEnd(evt);
                    break;
                case BattleEventType.DrawCard:
                    await playDrawCard(evt);
                    break;
                case BattleEventType.DiscardCard:
                    await playDiscardCard(evt);
                    break;
                case BattleEventType.CardMoved:
                    await playCardMoved(evt);
                    break;
                case BattleEventType.MergeCard:
                    await playMergeCard(evt);
                    break;
                case BattleEventType.GrantUltimate:
                    await playGrantUltimate(evt);
                    break;
                case BattleEventType.ShuffleDeck:
                    await playShuffleDeck(evt);
                    break;
                case BattleEventType.EnqueueCard:
                    await playEnqueueCard(evt);
                    break;
                case BattleEventType.DamageTaken:
                    await playDamageTaken(evt);
                    break;
                case BattleEventType.HealTaken:
                    await playHealTaken(evt);
                    break;
                case BattleEventType.EntityDied:
                    await playEntityDied(evt);
                    break;
                case BattleEventType.ActionPointChanged:
                    await playActionPointChanged(evt);
                    break;
            }
        }

        // ==================== 战斗流程事件 ====================
        // 战斗开始
        private static async Task playBattleStart(BattleEvent evt)
        {
#if UNITY_EDITOR
            Debug.Log($"[PlayEvent] 战斗开始，关卡: {evt.BattleStart.LevelId}");
#endif
            await Task.Delay(100);
        }

        // 战斗结束
        private static async Task playBattleEnd(BattleEvent evt)
        {
            bool isWin = evt.BattleEnd.IsPlayerWin;
#if UNITY_EDITOR
            Debug.Log($"[PlayEvent] 战斗结束，玩家{(isWin ? "胜利" : "失败")}");
#endif
            GameApp.MessageCenter.PostEvent(EventDefines.OpenFightSettleView, isWin);
            await Task.Delay(100);
        }

        // 回合开始
        private static async Task playTurnStart(BattleEvent evt)
        {
#if UNITY_EDITOR
            Debug.Log($"[PlayEvent] 回合开始，轮数: {evt.TurnStart.RoundNumber}");
#endif
            GameApp.MessageCenter.PostEvent(EventDefines.OnBattleTurnStart, evt.TurnStart.IsPlayerTurn);
            
            // 兼容旧版UI：玩家回合开始时触发 OnPlayerTurnStart
            if (evt.TurnStart.IsPlayerTurn)
            {
                GameApp.CardManager.CardActionQueue.Clear();
                GameApp.MessageCenter.PostEvent(EventDefines.OnPlayerTurnStart);
            }
            
            await Task.Delay(300);
        }

        // 回合结束
        private static async Task playTurnEnd(BattleEvent evt)
        {
#if UNITY_EDITOR
            Debug.Log($"[PlayEvent] 回合结束，轮数: {evt.TurnEnd.RoundNumber}");
#endif
            GameApp.MessageCenter.PostEvent(EventDefines.OnBattleTurnEnd, evt.TurnEnd.IsPlayerTurn);
            
            // 兼容旧版UI：玩家回合结束时触发 OnPlayerTurnOutput，驱动手牌隐藏等逻辑
            if (evt.TurnEnd.IsPlayerTurn)
            {
                var actions = GameApp.CardManager.CardActionQueue.GetAction();
                for (int i = 0; i < actions.Length; i++)
                {
                    if (actions[i].ActionType == CardActionType.PlayCard)
                    {
                        GameApp.MessageCenter.PostEvent(EventDefines.OnCardExecuteUI);
                        await Task.Delay(1150);
                    }
                    else if (actions[i].ActionType == CardActionType.MoveCard)
                    {
                        GameApp.MessageCenter.PostEvent(EventDefines.OnMoveActionExecute, i);
                        await Task.Delay(350);
                    }
                }
                GameApp.MessageCenter.PostEvent(EventDefines.OnPlayerTurnOutput);
            }
            
            await Task.Delay(200);
        }

        // ==================== 牌库与手牌事件 ====================
        // 抽牌
        private static async Task playDrawCard(BattleEvent evt)
        {
            var cardInfo = evt.DrawCard.Card;
            var deck = GameApp.CardManager.BattleContext.PlayerDecks[1];
            var card = new CardEntity(cardInfo.InstanceId, cardInfo.ConfigId, cardInfo.StarLevel);
            deck.HandCards.Add(card);

            GameApp.MessageCenter.PostEvent(EventDefines.UpdateHandCards, deck.HandCards);
            await Task.Delay(150);
        }

        // 弃牌
        private static async Task playDiscardCard(BattleEvent evt)
        {
            var cardInfo = evt.DiscardCard.Card;
            var deck = GameApp.CardManager.BattleContext.PlayerDecks[1];

            int index = deck.HandCards.FindIndex(c => c.InstanceId == cardInfo.InstanceId);
            if (index >= 0)
            {
                var card = deck.HandCards[index];
                card.StarLevel = 1;
                deck.HandCards.RemoveAt(index);
                deck.DiscardPile.Add(card);
            }

            GameApp.MessageCenter.PostEvent(EventDefines.UpdateHandCards, deck.HandCards);
            await Task.Delay(150);
        }

        // 移动卡牌
        private static async Task playCardMoved(BattleEvent evt)
        {
            var deck = GameApp.CardManager.BattleContext.PlayerDecks[1];
            int from = evt.MoveCard.FromIndex;
            int to = evt.MoveCard.ToIndex;

            if (from >= 0 && from < deck.HandCards.Count && to >= 0 && to < deck.HandCards.Count)
                (deck.HandCards[from], deck.HandCards[to]) = (deck.HandCards[to], deck.HandCards[from]);

            GameApp.MessageCenter.PostEvent(EventDefines.UpdateHandCards, deck.HandCards);
            await Task.Delay(150);
        }

        // 合成卡牌
        private static async Task playMergeCard(BattleEvent evt)
        {
            var deck = GameApp.CardManager.BattleContext.PlayerDecks[1];
            int slotIndex = evt.MergeCard.SlotIndex;
            int resultStar = evt.MergeCard.ResultStarLevel;

            if (slotIndex >= 0 && slotIndex < deck.HandCards.Count)
                deck.HandCards[slotIndex].StarLevel = resultStar;

            foreach (int consumedId in evt.MergeCard.ConsumedCardIds)
            {
                deck.HandCards.RemoveAll(c => c.InstanceId == consumedId);
            }

            GameApp.MessageCenter.PostEvent(EventDefines.OnHandCardMerged);
            GameApp.MessageCenter.PostEvent(EventDefines.UpdateHandCards, deck.HandCards);
            await Task.Delay(200);
        }

        // 发放大招牌
        private static async Task playGrantUltimate(BattleEvent evt)
        {
            var cardInfo = evt.GrantUltimate.Card;
            var deck = GameApp.CardManager.BattleContext.PlayerDecks[1];
            var card = new CardEntity(cardInfo.InstanceId, cardInfo.ConfigId, cardInfo.StarLevel);
            deck.HandCards.Insert(0, card);

            GameApp.MessageCenter.PostEvent(EventDefines.UpdateHandCards, deck.HandCards);
            await Task.Delay(150);
        }

        // 洗牌
        private static async Task playShuffleDeck(BattleEvent evt)
        {
#if UNITY_EDITOR
            Debug.Log("[PlayEvent] 牌库已洗牌");
#endif
            await Task.Delay(100);
        }

        // 卡牌进入行动队列
        private static async Task playEnqueueCard(BattleEvent evt)
        {
#if UNITY_EDITOR
            Debug.Log($"[PlayEvent] 卡牌进入行动队列，index: {evt.EnqueueCard.QueueIndex}");
#endif
            await Task.Delay(100);
        }

        // ==================== 实体状态事件 ====================
        // 受到伤害
        private static async Task playDamageTaken(BattleEvent evt)
        {
            var target = findCharacterByCombatInstanceId(evt.TargetId);
            if (target == null) return;

            target.TakeDamage(evt.Damage.DamageValue, evt.Damage.IsCritical);
            await Task.Delay(500);
        }

        // 批量受到伤害（同一张卡对多个目标时同时显示伤害字）
        private static async Task playDamageTakenBatch(List<BattleEvent> events)
        {
            foreach (var evt in events)
            {
                var target = findCharacterByCombatInstanceId(evt.TargetId);
                if (target != null)
                {
                    target.TakeDamage(evt.Damage.DamageValue, evt.Damage.IsCritical);
                }
            }
            await Task.Delay(500);
        }

        // 受到治疗
        private static async Task playHealTaken(BattleEvent evt)
        {
            var target = findCharacterByCombatInstanceId(evt.TargetId);
            if (target == null) return;

            target.CurrentHp = Mathf.Min(target.MaxHp, target.CurrentHp + evt.Heal.HealValue);
            target.HUD?.UpdateHp(target.CurrentHp, target.MaxHp);
            // TODO: 治疗数字飘字
            await Task.Delay(300);
        }

        // 实体死亡
        private static async Task playEntityDied(BattleEvent evt)
        {
            var target = findCharacterByCombatInstanceId(evt.TargetId);
            if (target == null) return;

            if (target.CurrentStateType != CharacterStateType.Die)
                target.ChangeState(CharacterStateType.Die);

            GameApp.MessageCenter.PostEvent(EventDefines.OnCharacterDie, target);
            await Task.Delay(500);
        }

        // 行动点变化
        private static async Task playActionPointChanged(BattleEvent evt)
        {
            var entityInfo = evt.ActionPointChanged;
            var hero = GameApp.EntityManager.GetCharacterById(entityInfo.EntityId) as HeroEntity;
            if (hero == null) return;

            hero.SetActionPoint(entityInfo.NewValue);
            hero.HUD?.UpdateActionPoint(entityInfo.NewValue);
            await Task.Delay(100);
        }

        // ==================== 工具函数 ====================
        // 根据 CombatInstanceId 查找角色
        private static BaseCharacter findCharacterByCombatInstanceId(int instanceId)
        {
            return GameApp.EntityManager.GetCharacterByCombatInstanceId(instanceId);
        }
    }
}
