/*
* ┌──────────────────────────────────┐
* │  描    述: 牌库与手牌事件播放器（抽牌/弃牌/移动/合成/大招/洗牌/入队）
* │  类    名: EventPlayer_CardDraw.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Common.Defines;
using Data.card;
using Data.card.Extensions;
using GameProtocol;
using Module.Character;
using Module.fight.Core.Entities;
using UnityEngine;

namespace Module.fight.EventPlayer
{
    public static class EventPlayer_CardDraw
    {
        // 获取当前客户端可见的事件归属牌库
        private static bool tryGetVisibleDeck(BattleEvent evt, out PlayerDeckEntity deck)
        {
            int eventOwnerId = evt.EventOwnerId == 0 ? 1 : evt.EventOwnerId;
            if (GameApp.PvpSession != null && GameApp.PvpSession.IsInPvp && eventOwnerId != GameApp.PvpSession.CurrentPlayerId)
            {
                deck = null;
                return false;
            }

            return GameApp.CardManager.BattleContext.PlayerDecks.TryGetValue(eventOwnerId, out deck);
        }

        public static async Task PlayDrawCard(BattleEvent evt)
        {
            var cardInfo = evt.DrawCard.Card;
            if (!tryGetVisibleDeck(evt, out var deck)) return;
            var card = new CardEntity(cardInfo.InstanceId, cardInfo.ConfigId, cardInfo.StarLevel);
            deck.HandCards.Insert(0, card);

            GameApp.MessageCenter.PostEvent(EventDefines.UpdateHandCards, deck.HandCards);
            await Task.Delay(150);
        }

        public static async Task PlayDrawCardBatch(List<BattleEvent> events)
        {
            if (events == null || events.Count == 0) return;

            PlayerDeckEntity updatedDeck = null;
            for (int i = 0; i < events.Count; i++)
            {
                var evt = events[i];
                if (!tryGetVisibleDeck(evt, out var deck)) continue;

                var cardInfo = evt.DrawCard.Card;
                if (deck.HandCards.Exists(c => c.InstanceId == cardInfo.InstanceId)) continue;

                var card = new CardEntity(cardInfo.InstanceId, cardInfo.ConfigId, cardInfo.StarLevel);
                deck.HandCards.Insert(0, card);
                updatedDeck = deck;
            }

            if (updatedDeck != null)
                GameApp.MessageCenter.PostEvent(EventDefines.UpdateHandCards, updatedDeck.HandCards);
            int delayMs = Mathf.Clamp(200 + events.Count * 70, 200, 900);
            await Task.Delay(delayMs);
        }

        public static async Task PlayDiscardCard(BattleEvent evt)
        {
            var cardInfo = evt.DiscardCard.Card;
            if (!tryGetVisibleDeck(evt, out var deck)) return;

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

        public static async Task PlayCardMoved(BattleEvent evt)
        {
            if (!tryGetVisibleDeck(evt, out var deck)) return;
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

        public static async Task PlayMergeCard(BattleEvent evt)
        {
            if (!tryGetVisibleDeck(evt, out var deck)) return;
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
                    GameApp.CardManager.EventBus.OnCardMerged?.Invoke(evt.EventOwnerId == 0 ? 1 : evt.EventOwnerId, keptCard, consumedCards[i], resultStar);
                }
            }

            await Task.Delay(800);
        }

        public static async Task PlayGrantUltimate(BattleEvent evt)
        {
            var cardInfo = evt.GrantUltimate.Card;
            if (!tryGetVisibleDeck(evt, out var deck)) return;
            var card = new CardEntity(cardInfo.InstanceId, cardInfo.ConfigId, cardInfo.StarLevel);
            deck.HandCards.Insert(0, card);

            GameApp.MessageCenter.PostEvent(EventDefines.UpdateHandCards, deck.HandCards);
            await Task.Delay(150);
        }

        public static async Task PlayShuffleDeck(BattleEvent evt)
        {
            QLog.Info("[PlayEvent] 牌库已洗牌");
            await Task.Delay(100);
        }

        public static async Task PlayEnqueueCard(BattleEvent evt)
        {
            QLog.Info($"[PlayEvent] 卡牌进入行动队列，index: {evt.EnqueueCard.QueueIndex}");
            if (PlayEventSequence.IsPlayerTurnResolving && PlayEventSequence.PendingPlayerPlayCardCount > 0)
            {
                GameApp.MessageCenter.PostEvent(EventDefines.OnCardExecuteUI);
                PlayEventSequence.PlayNextPlayerCasterAttack();
                PlayEventSequence.PendingPlayerPlayCardCount--;
                if (PlayEventSequence.PendingPlayerPlayCardCount <= 0)
                    PlayEventSequence.CompletePlayerTurnOutputIfNeeded();

                await Task.Delay(1150);
                return;
            }

            BaseCharacter sourceCharacter = EventPlayer_CharacterShift.FindCharacterByCombatInstanceId(evt.SourceId);
            bool isEnemySource = sourceCharacter != null && sourceCharacter.IsEnemySide;
            if (PlayEventSequence.IsEnemyTurnResolving || isEnemySource)
            {
                if (sourceCharacter != null && sourceCharacter.CurrentStateType != CharacterStateType.Die)
                    sourceCharacter.ChangeState(CharacterStateType.Attack);

                await Task.Delay(650);
                return;
            }

            var cardInfo = evt.EnqueueCard.Card;
            if (cardInfo != null)
            {
                if (!tryGetVisibleDeck(evt, out var deck)) return;
                deck.HandCards.RemoveAll(c => c.InstanceId == cardInfo.InstanceId);
            }

            await Task.Delay(100);
        }
    }
}
