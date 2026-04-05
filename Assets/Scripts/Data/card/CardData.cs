/*
* ┌──────────────────────────────────┐
* │  描    述: 卡牌数据                      
* │  类    名: CardData.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Data.card
{
    /// <summary>
    /// 卡牌类型
    /// </summary>
    public enum CardType
    {
        Attack,//攻击
        Buff,//给己方的增益
        Debuff,//给敌人的减益
        Channel,//吟诵(特殊)
        Health,//治疗
        Ultimate//大招
    }

    /// <summary>
    /// 卡牌效果
    /// </summary>
    [Serializable]
    public class CardEffect
    {
        public float value;//倍率等
        public int round;//持续回合数,如果是0则表示即时生效
        public string target;//目标,可以是单体/全体/随机n个等
    }
    
    [Serializable]
    [CreateAssetMenu(fileName = "NewCardData", menuName = "数据配置/Data/Card")]
    public class CardData : ScriptableObject
    {
        public CardType type;
        public int id;
        public string name;
        public string description;
        public int ownerId;//拥有者id

        public CardEffect[] effects;

        //TODO: 对玩家的属性进行增加以及特殊效果
    }
}