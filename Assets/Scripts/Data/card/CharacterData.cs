/*
* ┌──────────────────────────────────┐
* │  描    述: 角色数据                      
* │  类    名: CharacterData.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;

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
        public float attack;//攻击
        public float hp;//生命
        public float defense;//防御
        public float critRate;//暴击率
        public float critDamage;//暴击伤害
    }

    [Serializable]
    public class CharacterDataArray
    {
        public List<CharacterData> characters;
    }
    
    [Serializable]
    public class CharacterData
    {
        public int id;//角色id
        public string name;//角色名字
        public CharacterType characterType;//角色种类
        public InspirationType inspirationType;//角色的灵感种类
        
        public Property property;//角色属性

        public List<int> cards;//角色拥有的卡牌(一张大招牌和两张普通牌)

        public CharacterData()
        {
            
        }
        
        
        //TODO:实例化
    }
}