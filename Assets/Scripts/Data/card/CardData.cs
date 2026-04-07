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
    /// 效果类型
    /// </summary>
    public enum EffectType
    {
        Damage,//伤害
        Heal,//治疗
        Buff,//增益
        Debuff//减益
    }
    
    /// <summary>
    /// 作用对象
    /// </summary>
    public enum TargetType
    {
        Self,//己方
        Enemy//敌方
    }
    
    /// <summary>
    /// 卡牌效果
    /// </summary>
    [Serializable]
    public class CardEffect
    {
        public EffectType EffectType;
        public float Value;//倍率等
        public int Round;//持续回合数,如果是0则表示即时生效
        public TargetType Target;//目标
        
        [Tooltip("-1表示己方/敌方全部目标\n 0表示本身\n 其他数值则表示n个目标")]
        public int TargetCount = 1;//目标数量
    }
    
    [Serializable]
    [CreateAssetMenu(fileName = "NewCardData", menuName = "数据配置/Data/Card")]
    public class CardData : ScriptableObject
    {
        [Header("基本信息")]
        public CardType CardType;
        public int Id;
        public string Name;
        public string Description;
        public int OwnerId;//拥有者id

        [Header("效果列表")]
        public CardEffect[] Effects;

        [Header("视觉")]
        public Sprite CardSprite; //卡牌图片
        public GameObject CardEffectPrefab;//特效
        
    }
}