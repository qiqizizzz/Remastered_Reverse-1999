/*
* ┌──────────────────────────────────┐
* │  描    述: 角色数据                      
* │  类    名: CharacterData.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;

namespace Data.card
{
    /// <summary>
    /// 角色种类 - hero/monster
    /// </summary>
    public enum CharacterType
    {
        Hero,
        Monster
    }

    /// <summary>
    /// 灵感种类
    /// 克制关系为 兽 -> 木 -> 星 -> 岩 -> 兽
    /// </summary>
    public enum InspirationType
    {
        /// <summary>
        /// 兽
        /// </summary>
        Beast,
        /// <summary>
        /// 木
        /// </summary>
        Wood,
        /// <summary>
        /// 星
        /// </summary>
        Star,
        /// <summary>
        /// 岩
        /// </summary>
        Rock
    }

    /// <summary>
    /// 玩家属性
    /// </summary>
    [Serializable]
    public class Property
    {
        public float Hp;//生命
        public float Attack;//攻击
        public float Defense;//防御
        public float CritRate;//暴击率
        public float CritDamage;//暴击伤害
    }
    
    [Serializable]
    [CreateAssetMenu(fileName = "NewCharacterData", menuName = "数据配置/Data/Character")]
    public class CharacterData : ScriptableObject
    {
        [Header("基本信息")]
        public int Id;//角色id
        public string Name;//角色名字
        public string En_Name;
        public CharacterType CharacterType;//角色种类
        public InspirationType InspirationType;//角色的灵感种类
        
        [Header("属性与卡牌")]
        public Property Property;//角色属性
        public List<int> Cards;//角色拥有的卡牌(一张大招牌和两张普通牌)
        
        public List<CardData> GetAllCards()
        {
            return GameApp.ConfigManager.GetCharacterCards(Id);
        }
        
    }
}