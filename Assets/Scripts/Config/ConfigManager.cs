/*
* ┌──────────────────────────────────┐
* │  描    述: 配置管理器                      
* │  类    名: ConfigManager.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using System.Linq;
using Data.card;
using Module.level;
using UnityEngine;

namespace Config
{
    public class ConfigManager
    {
        private Dictionary<int, LevelData> levelConfig;
        private Dictionary<int, CharacterData> characterConfig;
        private Dictionary<int, CardData> cardConfig;

        public ConfigManager()
        {
            levelConfig = new Dictionary<int, LevelData>();
            characterConfig = new Dictionary<int, CharacterData>();
            cardConfig = new Dictionary<int, CardData>();
        }
        
        public void LoadAllConfigs()
        {
            //加载关卡
            var levelArray = JsonConfigLoader.Load<LevelDataArray>("LevelData");
            if (levelArray != null)
            {
                foreach (var level in levelArray.levels)
                {
                    levelConfig.Add(level.id, level);
                }
            }
            
            //加载卡牌
            var cardArray = JsonConfigLoader.Load<CardDataArray>("CardData");
            if (cardArray != null)
            {
                foreach (var card in cardArray.cards)
                {
                    cardConfig.Add(card.id, card);
                }
            }
            
            //加载角色
            var characterArray = JsonConfigLoader.Load<CharacterDataArray>("CharacterData");
            if (characterArray != null)
            {
                foreach (var character in characterArray.characters)
                {
                    characterConfig.Add(character.id, character);
                }
            }

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

        //得到某角色的全部卡牌
        public List<CardData> GetCharacterCards(int characterId)
        {
            List<CardData> cards = new List<CardData>();

            foreach (var card in cardConfig)
            {
                if (card.Value.ownerId == characterId)
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
        
        #region 关卡相关
        public LevelData GetLevelData(int levelId)
        {
            return levelConfig.TryGetValue(levelId, out LevelData levelData) ? levelData : null;
        }
        
        #endregion
    }
}