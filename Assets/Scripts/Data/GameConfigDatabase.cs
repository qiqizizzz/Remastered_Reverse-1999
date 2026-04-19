/*
* ┌──────────────────────────────────┐
* │  描    述: 游戏配置数据库                      
* │  类    名: GameConfigDatabase.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Data.card;
using UnityEngine;
using UnityEngine.Serialization;

namespace Data
{
    [CreateAssetMenu(fileName = "GameConfigDatabase", menuName = "数据配置/DataBase/GameConfigDatabase")]
    public class GameConfigDatabase : ScriptableObject
    {
        [Header("所有卡牌配置")]
        [FormerlySerializedAs("allCards")]
        public List<CardData> allCharacterCards = new List<CardData>();
        public List<CardData> allEnemyCards = new List<CardData>();

        [Header("所有角色/怪物配置")]
        public List<CharacterData> allCharacters = new List<CharacterData>();
        public List<CharacterData> allEnemies = new List<CharacterData>();

        [Header("所有关卡配置")]
        public List<LevelData> allLevels = new List<LevelData>();
        
    }
}