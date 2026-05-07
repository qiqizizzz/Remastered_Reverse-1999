using GameServer.Battle.Data;
using GameServer.Battle.Data.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Battle.Data
{
    internal class ConfigManager
    {
        public List<CardDataConfig> HeroCards { get; private set; }
        public List<CardDataConfig> EnemyCards { get; private set; }
        public List<CharacterDataConfig> Heroes { get; private set; }
        public List<CharacterDataConfig> Enemies { get; private set; }
        public List<LevelDataConfig> Levels { get; private set; }

        public void LoadAll(string configDir)
        {
            HeroCards = LoadList<CardDataConfig>(Path.Combine(configDir, "hero_cards.json"));
            EnemyCards = LoadList<CardDataConfig>(Path.Combine(configDir, "enemy_cards.json"));
            Heroes = LoadList<CharacterDataConfig>(Path.Combine(configDir, "heroes.json"));
            Enemies = LoadList<CharacterDataConfig>(Path.Combine(configDir, "enemies.json"));
            Levels = LoadList<LevelDataConfig>(Path.Combine(configDir, "levels.json"));
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
