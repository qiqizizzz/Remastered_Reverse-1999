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
using Common;
using GameProtocol;
using Module.Character;
using Module.fight.EventPlayer;
using UnityEngine;

namespace Module.fight
{
    public static class PlayEventSequence
    {
        private static bool _isPlaying;
        private static readonly Queue<IList<BattleEvent>> _eventQueue = new Queue<IList<BattleEvent>>();

        public static bool IsPlayerTurnResolving;
        public static bool IsEnemyTurnResolving;
        public static int PendingPlayerPlayCardCount;
        public static readonly Queue<int> PendingPlayerCasterOwnerIds = new Queue<int>();

        public static void Play(IList<BattleEvent> events)
        {
            if (events == null || events.Count == 0) return;

            _eventQueue.Enqueue(events);

            if (!_isPlaying)
                ProcessQueueAsync().ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
#if UNITY_EDITOR
                        Debug.LogError($"[PlayEventSequence] 队列异常终止: {t.Exception}");
#endif
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private static async Task ProcessQueueAsync()
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
                                batch.Add(events[++i]);
                            await EventPlayer_CardDraw.PlayDrawCardBatch(batch);
                        }
                        else if (evt.EventType == BattleEventType.DamageTaken)
                        {
                            var batch = new List<BattleEvent> { evt };
                            while (i + 1 < events.Count && events[i + 1].EventType == BattleEventType.DamageTaken)
                                batch.Add(events[++i]);
                            await EventPlayer_Damage.PlayDamageTakenBatch(batch);
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
                EventPlayer_CharacterShift.CompletePlayerTurnOutputIfNeeded();
                _isPlaying = false;
            }
        }

        private static async Task playSingleEvent(BattleEvent evt)
        {
            switch (evt.EventType)
            {
                case BattleEventType.BattleStart:        await EventPlayer_CharacterShift.PlayBattleStart(evt); break;
                case BattleEventType.BattleEnd:          await EventPlayer_CharacterShift.PlayBattleEnd(evt); break;
                case BattleEventType.TurnStart:          await EventPlayer_CharacterShift.PlayTurnStart(evt); break;
                case BattleEventType.TurnEnd:            await EventPlayer_CharacterShift.PlayTurnEnd(evt); break;
                case BattleEventType.DrawCard:           await EventPlayer_CardDraw.PlayDrawCard(evt); break;
                case BattleEventType.DiscardCard:        await EventPlayer_CardDraw.PlayDiscardCard(evt); break;
                case BattleEventType.CardMoved:          await EventPlayer_CardDraw.PlayCardMoved(evt); break;
                case BattleEventType.MergeCard:          await EventPlayer_CardDraw.PlayMergeCard(evt); break;
                case BattleEventType.GrantUltimate:      await EventPlayer_CardDraw.PlayGrantUltimate(evt); break;
                case BattleEventType.ShuffleDeck:        await EventPlayer_CardDraw.PlayShuffleDeck(evt); break;
                case BattleEventType.EnqueueCard:        await EventPlayer_CardDraw.PlayEnqueueCard(evt); break;
                case BattleEventType.DamageTaken:        await EventPlayer_Damage.PlayDamageTaken(evt); break;
                case BattleEventType.HealTaken:          await EventPlayer_Damage.PlayHealTaken(evt); break;
                case BattleEventType.EntityDied:         await EventPlayer_CharacterDie.PlayEntityDied(evt); break;
                case BattleEventType.ActionPointChanged: await EventPlayer_CharacterDie.PlayActionPointChanged(evt); break;
            }
        }

        #region 公共方法
        public static void CompletePlayerTurnOutputIfNeeded()
            => EventPlayer_CharacterShift.CompletePlayerTurnOutputIfNeeded();

        public static void PlayNextPlayerCasterAttack()
            => EventPlayer_CharacterShift.PlayNextPlayerCasterAttack();

        public static BaseCharacter FindCharacterByCombatInstanceId(int instanceId)
            => EventPlayer_CharacterShift.FindCharacterByCombatInstanceId(instanceId);

        public static void SyncCharacters(IList<CombatEntityInfo> entities)
            => EventPlayer_CharacterShift.SyncCharacters(entities);
        #endregion
    }
}
