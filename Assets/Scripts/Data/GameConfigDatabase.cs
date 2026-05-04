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
        public List<CardDataSO> allHeroCards = new List<CardDataSO>();
        public List<CardDataSO> allEnemyCards = new List<CardDataSO>();

        [Header("所有角色/怪物配置")]
        [FormerlySerializedAs("allCharacters")]
        public List<CharacterDataSO> allHeroes = new List<CharacterDataSO>();
        public List<CharacterDataSO> allEnemies = new List<CharacterDataSO>();

        [Header("所有关卡配置")]
        public List<LevelDataSO> allLevels = new List<LevelDataSO>();
        
    }
}