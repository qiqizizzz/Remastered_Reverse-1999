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
using Data.card;
using Data.card.Extensions;
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
        private static bool _isPlayerTurnResolving;
        private static bool _isEnemyTurnResolving;
        private static int _pendingPlayerPlayCardCount;
        private static readonly Queue<int> _pendingPlayerCasterOwnerIds = new Queue<int>();

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
                        if (evt.EventType == BattleEventType.DrawCard)
                        {
                            var batch = new List<BattleEvent> { evt };
                            while (i + 1 < events.Count && events[i + 1].EventType == BattleEventType.DrawCard)
                            {
                                batch.Add(events[++i]);
                            }
                            await playDrawCardBatch(batch);
                        }
                        else if (evt.EventType == BattleEventType.DamageTaken)
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
                completePlayerTurnOutputIfNeeded();
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

        #region 战斗流程事件
        // 战斗开始
        private static async Task playBattleStart(BattleEvent evt)
        {
            syncCharactersByBattleStart(evt.BattleStart.Entities);
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
            completePlayerTurnOutputIfNeeded();
#if UNITY_EDITOR
            Debug.Log($"[PlayEvent] 回合开始，轮数: {evt.TurnStart.RoundNumber}");
#endif
            GameApp.MessageCenter.PostEvent(EventDefines.OnBattleTurnStart, evt.TurnStart.IsPlayerTurn);
            
            // 兼容旧版UI：玩家回合开始时触发 OnPlayerTurnStart
            if (evt.TurnStart.IsPlayerTurn)
            {
                _isEnemyTurnResolving = false;
                GameApp.CardManager.CardActionQueue.Clear();
                GameApp.MessageCenter.PostEvent(EventDefines.OnPlayerTurnStart);
            }
            else
            {
                _isEnemyTurnResolving = true;
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
            
            // 玩家回合结束时，改为由服务端结算标记（EnqueueCard）逐张驱动出牌动画
            if (evt.TurnEnd.IsPlayerTurn)
            {
                refreshAllHeroActionPointConstant();
                var actions = GameApp.CardManager.CardActionQueue.GetAction();
                _isPlayerTurnResolving = true;
                _pendingPlayerPlayCardCount = 0;
                _pendingPlayerCasterOwnerIds.Clear();

                for (int i = 0; i < actions.Length; i++)
                {
                    if (actions[i].ActionType == CardActionType.PlayCard)
                    {
                        _pendingPlayerPlayCardCount++;
                        if (actions[i].cardEntity != null)
                        {
                            var cardConfig = actions[i].cardEntity.GetConfig();
                            if (cardConfig != null)
                                _pendingPlayerCasterOwnerIds.Enqueue(cardConfig.OwnerId);
                        }
                    }
                    else if (actions[i].ActionType == CardActionType.MoveCard)
                    {
                        GameApp.MessageCenter.PostEvent(EventDefines.OnMoveActionExecute, i);
                        await Task.Delay(350);
                    }
                }

                if (_pendingPlayerPlayCardCount == 0)
                    completePlayerTurnOutputIfNeeded();
            }
            else
            {
                _isEnemyTurnResolving = false;
            }
            
            await Task.Delay(200);
        }
        #endregion

        #region 牌库与手牌事件
        // 抽牌
        private static async Task playDrawCard(BattleEvent evt)
        {
            var cardInfo = evt.DrawCard.Card;
            var deck = GameApp.CardManager.BattleContext.PlayerDecks[1];
            var card = new CardEntity(cardInfo.InstanceId, cardInfo.ConfigId, cardInfo.StarLevel);
            deck.HandCards.Insert(0, card);

            GameApp.MessageCenter.PostEvent(EventDefines.UpdateHandCards, deck.HandCards);
            await Task.Delay(150);
        }

        // 批量抽牌（用于进关首轮发牌，避免逐张刷新导致中途位移）
        private static async Task playDrawCardBatch(List<BattleEvent> events)
        {
            if (events == null || events.Count == 0) return;

            var deck = GameApp.CardManager.BattleContext.PlayerDecks[1];
            for (int i = 0; i < events.Count; i++)
            {
                var cardInfo = events[i].DrawCard.Card;
                if (deck.HandCards.Exists(c => c.InstanceId == cardInfo.InstanceId)) continue;

                var card = new CardEntity(cardInfo.InstanceId, cardInfo.ConfigId, cardInfo.StarLevel);
                deck.HandCards.Insert(0, card);
            }

            GameApp.MessageCenter.PostEvent(EventDefines.UpdateHandCards, deck.HandCards);
            int delayMs = Mathf.Clamp(200 + events.Count * 70, 200, 900);
            await Task.Delay(delayMs);
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

            if (from >= 0 && from < deck.HandCards.Count && to >= 0 && to < deck.HandCards.Count && from != to)
            {
                CardEntity moveCard = deck.HandCards[from];
                deck.HandCards.RemoveAt(from);
                deck.HandCards.Insert(to, moveCard);
            }

            GameApp.MessageCenter.PostEvent(EventDefines.UpdateHandCards, deck.HandCards);
            await Task.Delay(150);
        }

        // 合成卡牌
        private static async Task playMergeCard(BattleEvent evt)
        {
            var deck = GameApp.CardManager.BattleContext.PlayerDecks[1];
            int resultStar = evt.MergeCard.ResultStarLevel;
            int resultCardInstanceId = evt.MergeCard.ResultCardInstanceId;

            CardEntity keptCard = deck.HandCards.Find(c => c.InstanceId == resultCardInstanceId);
            if (keptCard != null)
                keptCard.StarLevel = resultStar;

            List<CardEntity> consumedCards = new List<CardEntity>();
            foreach (int consumedId in evt.MergeCard.ConsumedCardIds)
            {
                CardEntity consumedCard = deck.HandCards.Find(c => c.InstanceId == consumedId);
                if (consumedCard != null)
                    consumedCards.Add(consumedCard);
            }

            foreach (int consumedId in evt.MergeCard.ConsumedCardIds)
            {
                deck.HandCards.RemoveAll(c => c.InstanceId == consumedId);
            }

            if (keptCard != null)
            {
                for (int i = 0; i < consumedCards.Count; i++)
                {
                    GameApp.CardManager.EventBus.OnCardMerged?.Invoke(1, keptCard, consumedCards[i], resultStar);
                }
            }

            await Task.Delay(800);
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
            if (_isPlayerTurnResolving && _pendingPlayerPlayCardCount > 0)
            {
                GameApp.MessageCenter.PostEvent(EventDefines.OnCardExecuteUI);
                playNextPlayerCasterAttack();
                _pendingPlayerPlayCardCount--;
                if (_pendingPlayerPlayCardCount <= 0)
                    completePlayerTurnOutputIfNeeded();
                
                await Task.Delay(1150);
                return;
            }

            BaseCharacter sourceCharacter = findCharacterByCombatInstanceId(evt.SourceId);
            bool isEnemySource = sourceCharacter is EnemyEntity;
            if (_isEnemyTurnResolving || isEnemySource)
            {
                if (sourceCharacter != null && sourceCharacter.CurrentStateType != CharacterStateType.Die)
                    sourceCharacter.ChangeState(CharacterStateType.Attack);

                await Task.Delay(650);
                return;
            }

            var cardInfo = evt.EnqueueCard.Card;
            if (cardInfo != null)
            {
                var deck = GameApp.CardManager.BattleContext.PlayerDecks[1];
                deck.HandCards.RemoveAll(c => c.InstanceId == cardInfo.InstanceId);
            }

            await Task.Delay(100);
        }
        #endregion

        #region 实体状态事件
        // 受到伤害
        private static async Task playDamageTaken(BattleEvent evt)
        {
            var target = findCharacterByCombatInstanceId(evt.TargetId);
            if (target == null) return;

            target.TakeDamage(evt.Damage.DamageValue, evt.Damage.IsCritical);
            tryPlayCardVfx(evt.SourceCardConfigId, target);
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
                    tryPlayCardVfx(evt.SourceCardConfigId, target);
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
            tryPlayCardVfx(evt.SourceCardConfigId, target);
            // TODO: 治疗数字飘字
            await Task.Delay(300);
        }

        // 实体死亡

        private static void tryPlayCardVfx(int configId, BaseCharacter target)
        {
            if (configId <= 0 || target == null) return;
            
            var cardData = GameApp.ConfigManager.Card.GetCardById(configId);
            if (cardData != null && cardData.CardEffectPrefab != null)
            {
                GameObject vfx = UnityEngine.Object.Instantiate(cardData.CardEffectPrefab, target.transform.position, Quaternion.identity);
                UnityEngine.Object.Destroy(vfx, 3f); 
            }
        }

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

            if (GameApp.CardManager.BattleContext.Entities.TryGetValue(entityInfo.EntityId, out var contextEntity))
                contextEntity.ActionPoint = entityInfo.NewValue;

            if (_isPlayerTurnResolving || _isEnemyTurnResolving)
            {
                hero.HUD?.UpdateActionPoint(entityInfo.NewValue, 0);
            }
            else
            {
                int previewGain = 0;
                CardAction[] queuedActions = GameApp.CardManager.CardActionQueue.GetAction();
                for (int i = 0; i < queuedActions.Length; i++)
                {
                    CardAction action = queuedActions[i];
                    if (action.ActionType != CardActionType.PlayCard || action.cardEntity == null) continue;
                    var config = action.cardEntity.GetConfig();
                    if (config == null || config.CardType == CardType.Ultimate) continue;
                    if (config.OwnerId == hero.CharacterData.Id) previewGain++;
                }

                int confirmedActionPoint = Mathf.Max(0, entityInfo.NewValue - previewGain);
                hero.HUD?.UpdateActionPoint(confirmedActionPoint, previewGain);
            }
            await Task.Delay(100);
        }
        #endregion

        #region 工具函数
        // 完成玩家输出阶段并触发手牌区收尾
        private static void completePlayerTurnOutputIfNeeded()
        {
            if (!_isPlayerTurnResolving) return;

            _isPlayerTurnResolving = false;
            _isEnemyTurnResolving = false;
            _pendingPlayerPlayCardCount = 0;
            _pendingPlayerCasterOwnerIds.Clear();
            refreshAllHeroActionPointConstant();
            GameApp.MessageCenter.PostEvent(EventDefines.OnPlayerTurnOutput);
        }

        // 触发下一张玩家出牌对应角色的攻击动作
        private static void playNextPlayerCasterAttack()
        {
            if (_pendingPlayerCasterOwnerIds.Count <= 0) return;

            int ownerId = _pendingPlayerCasterOwnerIds.Dequeue();
            BaseCharacter caster = GameApp.EntityManager.GetCharacterById(ownerId);
            if (caster == null || caster.CurrentStateType == CharacterStateType.Die) return;

            caster.ChangeState(CharacterStateType.Attack);
        }

        // 将所有英雄行动点恢复为常亮状态
        private static void refreshAllHeroActionPointConstant()
        {
            var heroes = GameApp.EntityManager.GetAliveHeroes();
            for (int i = 0; i < heroes.Count; i++)
            {
                HeroEntity hero = heroes[i];
                hero.HUD?.UpdateActionPoint(hero.ActionPoint, 0);
            }
        }

        // 用 BattleStart 实体快照对齐本地角色与服务端 InstanceId 映射
        private static void syncCharactersByBattleStart(IList<CombatEntityInfo> entities)
        {
            if (entities == null || entities.Count == 0) return;

            var heroes = new List<BaseCharacter>(GameApp.EntityManager.GetAliveHeroes());
            var enemies = new List<BaseCharacter>(GameApp.EntityManager.GetAliveEnemies());
            var usedHeroes = new HashSet<BaseCharacter>();
            var usedEnemies = new HashSet<BaseCharacter>();

            for (int i = 0; i < entities.Count; i++)
            {
                CombatEntityInfo info = entities[i];
                var candidates = info.IsPlayerSide ? heroes : enemies;
                var used = info.IsPlayerSide ? usedHeroes : usedEnemies;
                BaseCharacter best = null;
                float bestHpDiff = float.MaxValue;

                for (int j = 0; j < candidates.Count; j++)
                {
                    BaseCharacter character = candidates[j];
                    if (used.Contains(character)) continue;
                    if (character.CharacterData == null || character.CharacterData.Id != info.ConfigId) continue;

                    float hpDiff = Mathf.Abs(character.CurrentHp - info.CurrentHp);
                    if (hpDiff < bestHpDiff)
                    {
                        bestHpDiff = hpDiff;
                        best = character;
                    }
                }

                if (best == null) continue;
                used.Add(best);
                best.SetCombatInstanceId(info.InstanceId);
                best.SetHpFromSnapshot(info.CurrentHp, info.MaxHp);

                if (best is HeroEntity hero)
                {
                    hero.SetActionPoint(info.ActionPoint);
                    hero.HUD?.UpdateActionPoint(info.ActionPoint, 0);
                }
            }
        }

        // 根据 CombatInstanceId 查找角色
        private static BaseCharacter findCharacterByCombatInstanceId(int instanceId)
        {
            return GameApp.EntityManager.GetCharacterByCombatInstanceId(instanceId);
        }
        #endregion
    }
}
