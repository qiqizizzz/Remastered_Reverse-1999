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
using Data.level;
using Module.level;
using UnityEngine;

namespace Config
{
    public class ConfigManager
    {
        private Dictionary<int, LevelDataSO> levelConfig;
        private Dictionary<int, CharacterDataSO> characterConfig;
        private Dictionary<int, CardDataSO> cardConfig;
        
        private Dictionary<string, CharacterDataSO> characterConfigByName;

        public ConfigManager()
        {
            levelConfig = new Dictionary<int, LevelDataSO>();
            characterConfig = new Dictionary<int, CharacterDataSO>();
            cardConfig = new Dictionary<int, CardDataSO>();
            characterConfigByName = new Dictionary<string, CharacterDataSO>();
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
                if (level != null)
                    levelConfig.TryAdd(level.Id, level);
            }
            
            foreach (var character in db.allCharacters)
            {
                if (character != null)
                    characterConfig.TryAdd(character.Id, character);
            }

            foreach (var enemy in db.allEnemies)
            {
                if(enemy != null)
                    characterConfig.TryAdd(enemy.Id, enemy);
            }
            
            foreach (var card in db.allCharacterCards)
            {
                if (card != null)
                    cardConfig.TryAdd(card.Id, card);
            }

            foreach (var card in db.allEnemyCards)
            {
                if (card != null)
                    cardConfig.TryAdd(card.Id, card);
            }
            
            //根据角色名字建立一个快速查询的字典
            foreach (var kvp in characterConfig)
            {
                if (!characterConfigByName.ContainsKey(kvp.Value.Name))
                    characterConfigByName.Add(kvp.Value.Name, kvp.Value);
            }
            
            Debug.Log($"加载完成: {cardConfig.Count}张卡牌, {characterConfig.Count}个角色, {levelConfig.Count}个关卡");
        }

        #region 角色/卡牌相关
        public CardDataSO GetCardData(int cardId)
        {
            return cardConfig.TryGetValue(cardId, out CardDataSO cardData) ? cardData : null;
        }

        public CharacterDataSO GetCharacterData(int characterId)
        {
            return characterConfig.TryGetValue(characterId, out CharacterDataSO characterData) ? characterData : null;
        }

        public CharacterDataSO GetCharacterData(string characterName)
        {
            return characterConfigByName.TryGetValue(characterName, out CharacterDataSO characterData) ? characterData : null;
        }
        
        //得到某角色的全部卡牌
        public List<CardDataSO> GetCharacterCards(int characterId)
        {
            List<CardDataSO> cards = new List<CardDataSO>();

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
        public List<CharacterDataSO> GetAllCharacters()
        {
            return characterConfig.Values.Where(c => c.CharacterType == CharacterType.Hero).ToList();
        }
        
        #endregion

        #region 敌人相关
        public CharacterDataSO GetEnemyData(int enemyId)
        {
            return characterConfig.TryGetValue(enemyId, out CharacterDataSO enemyData) ? enemyData : null;
        }

        #endregion
        
        #region 关卡相关
        public LevelDataSO GetLevelData(int levelId)
        {
            return levelConfig.TryGetValue(levelId, out LevelDataSO levelData) ? levelData : null;
        }
        
        public List<MonsterSpawnData> GetLevelMonsterSpawnData(int levelId)
        {
            if (levelConfig.TryGetValue(levelId, out LevelDataSO levelData))
            {
                return levelData.MonsterSpawns;
            }
            return new List<MonsterSpawnData>();
        }
        
        #endregion
    }
}