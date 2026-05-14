using GameServer.Common;
using GameServer.Battle.Data;
using GameServer.Battle.Data.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Battle.Data
{
    internal class ConfigManager : ICardCatalog
    {
        public List<CardDataConfig> HeroCards { get; private set; }
        public List<CardDataConfig> EnemyCards { get; private set; }
        public List<CharacterDataConfig> Heroes { get; private set; }
        public List<CharacterDataConfig> Enemies { get; private set; }
        public List<LevelDataConfig> Levels { get; private set; }

        private Dictionary<int, CharacterDataConfig> _heroDict;
        private Dictionary<int, CharacterDataConfig> _enemyDict;
        private Dictionary<int, CardDataConfig> _cardDict;
        private Dictionary<int, List<CardDataConfig>> _cardByOwnerDict;

        public void LoadAll(string configDir)
        {
            try
            {
                HeroCards = LoadList<CardDataConfig>(Path.Combine(configDir, "hero_cards.json"));
                EnemyCards = LoadList<CardDataConfig>(Path.Combine(configDir, "enemy_cards.json"));
                Heroes = LoadList<CharacterDataConfig>(Path.Combine(configDir, "heroes.json"));
                Enemies = LoadList<CharacterDataConfig>(Path.Combine(configDir, "enemies.json"));
                Levels = LoadList<LevelDataConfig>(Path.Combine(configDir, "levels.json"));

                _heroDict = Heroes.ToDictionary(h => h.Id);
                _enemyDict = Enemies.ToDictionary(e => e.Id);

                var allCards = new List<CardDataConfig>();
                allCards.AddRange(HeroCards);
                allCards.AddRange(EnemyCards);
                _cardDict = allCards.ToDictionary(c => c.Id);

                _cardByOwnerDict = new Dictionary<int, List<CardDataConfig>>();
                foreach (var card in allCards)
                {
                    if (!_cardByOwnerDict.TryGetValue(card.OwnerId, out var list))
                    {
                        list = new List<CardDataConfig>();
                        _cardByOwnerDict[card.OwnerId] = list;
                    }
                    list.Add(card);
                }

                QLog.Info($"[ConfigManager] 配置加载完成: HeroCards={HeroCards.Count}, EnemyCards={EnemyCards.Count}, Heroes={Heroes.Count}, Enemies={Enemies.Count}, Levels={Levels.Count}");
            }
            catch (Exception ex)
            {
                QLog.Info($"[ConfigManager] 配置加载失败: {ex.Message}");
                throw;
            }
        }

        // 根据角色配置Id查找角色数据
        public CharacterDataConfig GetCharacter(int id)
        {
            if (_heroDict != null && _heroDict.TryGetValue(id, out var hero)) return hero;
            if (_enemyDict != null && _enemyDict.TryGetValue(id, out var enemy)) return enemy;
            return null;
        }

        // ICardCatalog 实现：根据卡牌配置Id查找卡牌数据
        public CardDataConfig Get(int id)
        {
            if (_cardDict != null && _cardDict.TryGetValue(id, out var card)) return card;
            return null;
        }

        // ICardCatalog 实现：根据角色配置Id获取该角色的所有卡牌
        public IReadOnlyList<CardDataConfig> GetCharacterCards(int characterId)
        {
            if (_cardByOwnerDict != null && _cardByOwnerDict.TryGetValue(characterId, out var cards))
                return cards;
            return Array.Empty<CardDataConfig>();
        }

        private List<T> LoadList<T>(string path)
        {
            string json = File.ReadAllText(path);
            var wrapper = System.Text.Json.JsonSerializer.Deserialize<JsonArrayWrapper<T>>(json);
            return wrapper.items.ToList();
        }
    }

    public class JsonArrayWrapper<T>
    {
        public T[] items { get; set; }
    }
}
