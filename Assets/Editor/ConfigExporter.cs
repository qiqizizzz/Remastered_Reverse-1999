/*
* ┌──────────────────────────────────┐
* │  描    述: 战斗配置导出工具，将SO数据导出为JSON供服务端使用
* │  类    名: ConfigExporter.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using System.IO;
using Data;
using Data.card;
using Data.level;
using UnityEditor;
using UnityEngine;

namespace Config.Editor
{
    // ==================== DTO定义（剥离Unity特有类型，仅保留纯数据） ====================

    [Serializable]
    public class CardDataJson
    {
        public int Id;
        public string Name;
        public string Description;
        public int OwnerId;
        public CardType CardType;
        public CardEffect[] Effects;
    }

    [Serializable]
    public class CharacterDataJson
    {
        public int Id;
        public string Name;
        public string En_Name;
        public CharacterType CharacterType;
        public InspirationType InspirationType;
        public Property Property;
        public List<int> Cards;
    }

    [Serializable]
    public class LevelDataJson
    {
        public int Id;
        public string Name;
        public string Description;
        public List<MonsterSpawnData> MonsterSpawns;
    }

    [Serializable]
    public class JsonArrayWrapper<T>
    {
        public T[] items;
    }

    // ==================== 导出工具 ====================

    public class ConfigExporter
    {
        private const string DATABASE_PATH = "Assets/RemoteAssets/Data/DataBase/GameConfigDatabase.asset";
        private const string OUTPUT_DIR = "Assets/Scripts/Config/json";

        [MenuItem("Tools/导出战斗配置到JSON")]
        public static void ExportBattleConfigs()
        {
            GameConfigDatabase db = AssetDatabase.LoadAssetAtPath<GameConfigDatabase>(DATABASE_PATH);
            if (db == null)
            {
                Debug.LogError($"[ConfigExporter] 未找到配置数据库: {DATABASE_PATH}");
                return;
            }

            if (!Directory.Exists(OUTPUT_DIR))
                Directory.CreateDirectory(OUTPUT_DIR);

            ExportHeroCards(db);
            ExportEnemyCards(db);
            ExportHeroes(db);
            ExportEnemies(db);
            ExportLevels(db);

            AssetDatabase.Refresh();
            Debug.Log("[ConfigExporter] 战斗配置导出完成");
        }

        private static void ExportHeroCards(GameConfigDatabase db)
        {
            var list = new List<CardDataJson>();
            foreach (var so in db.allHeroCards)
            {
                if (so == null) continue;
                list.Add(ToCardDataJson(so));
            }
            WriteJson("hero_cards.json", list);
        }

        private static void ExportEnemyCards(GameConfigDatabase db)
        {
            var list = new List<CardDataJson>();
            foreach (var so in db.allEnemyCards)
            {
                if (so == null) continue;
                list.Add(ToCardDataJson(so));
            }
            WriteJson("enemy_cards.json", list);
        }

        private static void ExportHeroes(GameConfigDatabase db)
        {
            var list = new List<CharacterDataJson>();
            foreach (var so in db.allHeroes)
            {
                if (so == null) continue;
                list.Add(ToCharacterDataJson(so));
            }
            WriteJson("heroes.json", list);
        }

        private static void ExportEnemies(GameConfigDatabase db)
        {
            var list = new List<CharacterDataJson>();
            foreach (var so in db.allEnemies)
            {
                if (so == null) continue;
                list.Add(ToCharacterDataJson(so));
            }
            WriteJson("enemies.json", list);
        }

        private static void ExportLevels(GameConfigDatabase db)
        {
            var list = new List<LevelDataJson>();
            foreach (var so in db.allLevels)
            {
                if (so == null) continue;
                list.Add(ToLevelDataJson(so));
            }
            WriteJson("levels.json", list);
        }

        private static CardDataJson ToCardDataJson(CardDataSO so)
        {
            return new CardDataJson
            {
                Id = so.Id,
                Name = so.Name,
                Description = so.Description,
                OwnerId = so.OwnerId,
                CardType = so.CardType,
                Effects = so.Effects ?? Array.Empty<CardEffect>()
            };
        }

        private static CharacterDataJson ToCharacterDataJson(CharacterDataSO so)
        {
            return new CharacterDataJson
            {
                Id = so.Id,
                Name = so.Name,
                En_Name = so.En_Name,
                CharacterType = so.CharacterType,
                InspirationType = so.InspirationType,
                Property = so.Property,
                Cards = so.Cards
            };
        }

        private static LevelDataJson ToLevelDataJson(LevelDataSO so)
        {
            return new LevelDataJson
            {
                Id = so.Id,
                Name = so.Name,
                Description = so.Description,
                MonsterSpawns = so.MonsterSpawns
            };
        }

        private static void WriteJson<T>(string fileName, List<T> list)
        {
            var wrapper = new JsonArrayWrapper<T> { items = list.ToArray() };
            string json = JsonUtility.ToJson(wrapper, true);
            string path = Path.Combine(OUTPUT_DIR, fileName);
            File.WriteAllText(path, json);
            Debug.Log($"[ConfigExporter] 已导出: {path} ({list.Count} 条)");
        }
    }
}
