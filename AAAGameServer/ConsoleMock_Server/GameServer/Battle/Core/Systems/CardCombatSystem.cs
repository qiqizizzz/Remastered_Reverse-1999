using GameServer.Battle.Core.Entities;
using GameServer.Battle.Core.EventBus;
using GameServer.Battle.Core.Extensions;
using GameServer.Battle.Data;
using GameServer.Battle.Data.Config;
using System.Diagnostics;

namespace GameServer.Battle.Core.Systems
{
    internal class CardCombatSystem
    {
        private readonly CombatContext _context;
        private readonly ICardCatalog _cardCatalog;
        private readonly CombatEventBus _eventBus;
        private readonly Random _random;

        private static int num = 0;

        public CardCombatSystem(CombatContext context, ICardCatalog cardCatalog, CombatEventBus eventBus)
        {
            _context = context;
            _cardCatalog = cardCatalog;
            _eventBus = eventBus;
            _random = new Random();
        }

        //初始化玩家牌库
        public void InitDeck(int playerId, List<int> CharacterConfigIds, int normalCardMaxLimit = 3)
        {
            if (!_context.PlayerDecks.TryGetValue(playerId, out var deck))
            {
                deck = new PlayerDeckEntity { PlayerId = playerId };
                _context.PlayerDecks[playerId] = deck;
            }

            deck.DrawPile.Clear();
            deck.HandCards.Clear();
            deck.DiscardPile.Clear();

            foreach (var charId in CharacterConfigIds)
            {
                var charCards = _cardCatalog.GetCharacterCards(charId);
                Console.WriteLine($"[InitDeck] 角色 {charId} 的卡牌数量: {charCards.Count}");

                foreach (var card in charCards)
                {
                    if (card.CardType == CardType.Ultimate) continue;

                    for (int i = 0; i < normalCardMaxLimit; i++)
                    {
                        deck.DrawPile.Add(new CardEntity(card.Id));
                    }
                }
            }

            ShuffleCard(deck.DrawPile);
            Console.WriteLine($"[InitDeck] 牌库初始化完成: DrawPile={deck.DrawPile.Count}, HandCards={deck.HandCards.Count}, DiscardPile={deck.DiscardPile.Count}");
        }

        #region 准备阶段与洗牌
        //为新关卡准备初始手牌（从牌堆中抽取）
        public void PrepareHandsForNewLevel(int playerId, List<int> CharacterConfigIds)
        {
            //回合开始时（即新进入关卡时）,玩家当前手牌应为每个角色两张普通牌,加起来一共8张（4个角色）
            if (!_context.PlayerDecks.TryGetValue(playerId, out var deck)) return;

            foreach (var charId in CharacterConfigIds)
            {
                var charCards = _cardCatalog.GetCharacterCards(charId);
                foreach (var card in charCards)
                {
                    if (card.CardType == CardType.Ultimate) continue;

                    // 优先从 DrawPile 中抽取该角色的牌
                    int drawIndex = deck.DrawPile.FindIndex(c => c.ConfigId == card.Id);
                    if (drawIndex >= 0)
                    {
                        var drawnCard = deck.DrawPile[drawIndex];
                        deck.DrawPile.RemoveAt(drawIndex);
                        deck.HandCards.Insert(0, drawnCard);
                        _eventBus?.OnCardDrawn?.Invoke(playerId, drawnCard);
                    }
                    else
                    {
                        // 兜底：若牌堆中无该牌则直接创建
                        var fallbackCard = new CardEntity(card.Id);
                        deck.HandCards.Insert(0, fallbackCard);
                        _eventBus?.OnCardDrawn?.Invoke(playerId, fallbackCard);
                    }
                }
            }

            ShuffleCard(deck.HandCards);
            _eventBus?.OnHandCardsUpdated?.Invoke(playerId, new List<CardEntity>(deck.HandCards));
            Console.WriteLine($"[PrepareHandsForNewLevel] 初始手牌准备完成: HandCards={deck.HandCards.Count}, DrawPile={deck.DrawPile.Count}");
        }

        //Fisher–Yates 洗牌算法
        public void ShuffleCard(List<CardEntity> pile)
        {
            for (int i = 0; i < pile.Count; i++)
            {
                int RandomIndex = _random.Next(i, pile.Count);
                (pile[i], pile[RandomIndex]) = (pile[RandomIndex], pile[i]);
            }
        }
        #endregion

        #region 卡牌主要操作
        //出牌
        public void PlayCard(int playerId, CardEntity card, int targetInstanceId)
        {

        }

        //抽牌
        public void DrawCard(int playerId, int count)
        {
            if (!_context.PlayerDecks.TryGetValue(playerId, out var deck)) return;

            int drawn = 0;
            for (int i = 0; i < count; i++)
            {
                if (deck.DrawPile.Count == 0)
                {
                    if (deck.DiscardPile.Count == 0)
                    {
                        Console.WriteLine($"[DrawCard] 抽牌失败: 牌堆({deck.DrawPile.Count})和弃牌堆({deck.DiscardPile.Count})都没有牌了, 请求抽{count}张, 实际抽了{drawn}张");
                        break;
                    }

                    deck.DrawPile.AddRange(deck.DiscardPile);
                    deck.DiscardPile.Clear();
                    ShuffleCard(deck.DrawPile);
                    Console.WriteLine($"[DrawCard] 弃牌堆洗入抽牌堆, 当前抽牌堆数量: {deck.DrawPile.Count}");
                }

                CardEntity drawnCard = deck.DrawPile[^1];
                deck.DrawPile.RemoveAt(deck.DrawPile.Count - 1);
                deck.HandCards.Insert(0, drawnCard);
                drawn++;
                _eventBus?.OnCardDrawn?.Invoke(playerId, drawnCard);
            }

            _eventBus?.OnHandCardsUpdated?.Invoke(playerId, new List<CardEntity>(deck.HandCards));
            Console.WriteLine($"[DrawCard] 抽牌完成: 请求{count}张, 实际{drawn}张, DrawPile={deck.DrawPile.Count}, HandCards={deck.HandCards.Count}, DiscardPile={deck.DiscardPile.Count}");
        }

        //弃牌
        public void DiscardCard(int playerId, CardEntity card)
        {
            if (!_context.PlayerDecks.TryGetValue(playerId, out var deck)) return;

            var config = _cardCatalog.Get(card.ConfigId);
            if (config.CardType == CardType.Ultimate) return;

            card.StarLevel = 1;
            deck.DiscardPile.Add(card);

            _eventBus?.OnCardDiscarded?.Invoke(playerId, card);
            _eventBus?.OnHandCardsUpdated?.Invoke(playerId, new List<CardEntity>(deck.HandCards));
        }

        //交换手牌
        public void SwapHandCards(int playerId, int indexA, int indexB)
        {
            if (!_context.PlayerDecks.TryGetValue(playerId, out var deck)) return;
            if (indexA < 0 || indexA >= deck.HandCards.Count || indexB < 0 || indexB >= deck.HandCards.Count) return;

            (deck.HandCards[indexA], deck.HandCards[indexB]) = (deck.HandCards[indexB], deck.HandCards[indexA]);

            _eventBus?.OnHandCardSwapped?.Invoke(playerId, indexA, indexB);
            _eventBus?.OnHandCardsUpdated?.Invoke(playerId, new List<CardEntity>(deck.HandCards));

            // 交换后检查合成
            _context.CheckAndAutoMerge(playerId);
        }

        //发放大招牌
        public bool TryGiveUltimateCard(int playerId, int characterConfigId)
        {
            if (!_context.PlayerDecks.TryGetValue(playerId, out var deck)) return false;

            foreach (var card in deck.HandCards)
            {
                var config = _cardCatalog.Get(card.ConfigId);
                if (config.CardType == CardType.Ultimate) return false;
            }


            var charCards = _cardCatalog.GetCharacterCards(characterConfigId);
            //寻找大招
            foreach (var cardData in charCards)
            {
                if (cardData.CardType == CardType.Ultimate)
                {
                    deck.HandCards.Insert(0, new CardEntity(cardData.Id));

                    _eventBus?.OnUltimateCardGranted?.Invoke(playerId, characterConfigId, new CardEntity(cardData.Id));
                    _eventBus?.OnHandCardsUpdated?.Invoke(playerId, new List<CardEntity>(deck.HandCards));

                    return true;
                }
            }

            return false;
        }

        //移除角色卡牌
        public List<CardEntity> RemoveCardsOfCharacter(int playerId, int characterConfigId)
        {
            if (!_context.PlayerDecks.TryGetValue(playerId, out var deck)) return new List<CardEntity>();

            deck.DrawPile.RemoveAll(c => _cardCatalog.Get(c.ConfigId).OwnerId == characterConfigId);
            deck.DiscardPile.RemoveAll(c => _cardCatalog.Get(c.ConfigId).OwnerId == characterConfigId);

            List<CardEntity> removedHandCards =
                deck.HandCards.FindAll(c => _cardCatalog.Get(c.ConfigId).OwnerId == characterConfigId);
            if (removedHandCards.Count > 0)
            {
                deck.HandCards.RemoveAll(c => _cardCatalog.Get(c.ConfigId).OwnerId == characterConfigId);
            }

            _eventBus?.OnCharacterCardsRemoved?.Invoke(playerId, characterConfigId, removedHandCards);
            _eventBus?.OnHandCardsUpdated?.Invoke(playerId, new List<CardEntity>(deck.HandCards));

            return removedHandCards;
        }
        #endregion
    }
}
