/*
* ┌──────────────────────────────────┐
* │  描    述: 配置管理器                      
* │  类    名: ConfigManager.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Defines;
using Data;
using Data.card;
using Module.level;
using UnityEngine;

namespace Config
{
    public class ConfigManager
    {
        private Dictionary<int, LevelData> levelConfig;
        private Dictionary<int, CharacterData> characterConfig;
        private Dictionary<int, CharacterData> enemyConfig;
        private Dictionary<int, CardData> cardConfig;
        
        private Dictionary<string, CharacterData> characterConfigByName;

        public ConfigManager()
        {
            levelConfig = new Dictionary<int, LevelData>();
            characterConfig = new Dictionary<int, CharacterData>();
            enemyConfig = new Dictionary<int, CharacterData>();
            cardConfig = new Dictionary<int, CardData>();
            characterConfigByName = new Dictionary<string, CharacterData>();
        }

        public async Task LoadAllConfigsAsync()
        {
            var tcs = new TaskCompletionSource<GameConfigDatabase>();
            
            ResManager.LoadAssetAsync<GameConfigDatabase>(AddressDefines.Data_GameConfigDatabase, database =>
            {
                tcs.SetResult(database);
            });
            
            GameConfigDatabase db = await tcs.Task;

            if (db == null)
            {
                Debug.LogError("加载配置数据库失败!");
                return;
            }
            
            levelConfig.Clear();
            characterConfig.Clear();
            cardConfig.Clear();

            //加载关卡、角色、卡牌数据
            foreach (var level in db.allLevels)
            {
                if (level != null && !levelConfig.ContainsKey(level.Id))
                    levelConfig.Add(level.Id, level);
            }
            
            foreach (var character in db.allCharacters)
            {
                if (character != null && !characterConfig.ContainsKey(character.Id))
                    characterConfig.Add(character.Id, character);
            }

            foreach (var enemy in db.allEnemies)
            {
                if(enemy != null && !enemyConfig.ContainsKey(enemy.Id))
                    enemyConfig.Add(enemy.Id, enemy);
            }
            
            foreach (var card in db.allCards)
            {
                if (card != null && !cardConfig.ContainsKey(card.Id))
                    cardConfig.Add(card.Id, card);
            }
            
            //根据角色名字建立一个快速查询的字典
            foreach (var kvp in characterConfig)
            {
                if (!characterConfigByName.ContainsKey(kvp.Value.En_Name))
                    characterConfigByName.Add(kvp.Value.En_Name, kvp.Value);
            }
            
            Debug.Log($"加载完成: {cardConfig.Count}张卡牌, {characterConfig.Count}个角色, {levelConfig.Count}个关卡");
        }
        
        public void LoadAllConfigs()
        {
            /*//加载关卡
            var levelArray = JsonConfigLoader.Load<LevelDataArray>("LevelData");
            if (levelArray != null)
            {
                foreach (var level in levelArray.levels)
                {
                    levelConfig.Add(level.id, level);
                }
            }*/
            
            /*//加载卡牌
            var cardArray = JsonConfigLoader.Load<CardDataArray>("CardData");
            if (cardArray != null)
            {
                foreach (var card in cardArray.cards)
                {
                    cardConfig.Add(card.id, card);
                }
            }*/
            
            /*//加载角色
            var characterArray = JsonConfigLoader.Load<CharacterDataArray>("CharacterData");
            if (characterArray != null)
            {
                foreach (var character in characterArray.characters)
                {
                    characterConfig.Add(character.id, character);
                }
            }*/

            Debug.Log($"加载完成: {cardConfig.Count}张卡牌, {characterConfig.Count}个角色, {levelConfig.Count}个关卡");
        }

        #region 角色/卡牌相关
        public CardData GetCardData(int cardId)
        {
            return cardConfig.TryGetValue(cardId, out CardData cardData) ? cardData : null;
        }

        public CharacterData GetCharacterData(int characterId)
        {
            return characterConfig.TryGetValue(characterId, out CharacterData characterData) ? characterData : null;
        }

        public CharacterData GetCharacterData(string characterName)
        {
            return characterConfigByName.TryGetValue(characterName, out CharacterData characterData) ? characterData : null;
        }
        
        //得到某角色的全部卡牌
        public List<CardData> GetCharacterCards(int characterId)
        {
            List<CardData> cards = new List<CardData>();

            foreach (var card in cardConfig)
            {
                if (card.Value.OwnerId == characterId)
                {
                    cards.Add(card.Value);
                }
            }
            
            return cards;
        }
        
        //得到所有角色
        public List<CharacterData> GetAllCharacters()
        {
            return characterConfig.Values.ToList();
        }
        
        #endregion

        #region 敌人相关
        public CharacterData GetEnemyData(int enemyId)
        {
            return enemyConfig.TryGetValue(enemyId, out CharacterData enemyData) ? enemyData : null;
        }

        #endregion
        
        #region 关卡相关
        public LevelData GetLevelData(int levelId)
        {
            return levelConfig.TryGetValue(levelId, out LevelData levelData) ? levelData : null;
        }
        
        public List<MonsterSpawnData> GetLevelMonsterSpawnData(int levelId)
        {
            if (levelConfig.TryGetValue(levelId, out LevelData levelData))
            {
                return levelData.MonsterSpawns;
            }
            return new List<MonsterSpawnData>();
        }
        
        #endregion
    }
}