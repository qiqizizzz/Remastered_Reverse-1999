/*
* ┌──────────────────────────────────┐
* │  描    述: 配置管理器                      
* │  类    名: ConfigManager.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Data.card;
using UnityEngine;

namespace Config
{
    public class ConfigManager
    {
        private Dictionary<int, CharacterData> characterConfig;
        private Dictionary<int, CardData> cardConfig;

        public ConfigManager()
        {
            characterConfig = new Dictionary<int, CharacterData>();
            cardConfig = new Dictionary<int, CardData>();
        }
        
        public void LoadAllConfigs()
        {
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
            
            Debug.Log($"加载完成: {cardConfig.Count}张卡牌, {characterConfig.Count}个角色");
        }

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
    }
}