/*
* ┌──────────────────────────────────┐
* │  描    述: 战斗核心专注的局域事件总线                      
* │  类    名: CombatEventBus.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Module.fight.Core.Entities;

namespace Module.fight.Core.EventBus
{
    public class CombatEventBus
    {
        #region 手牌整体状态
        /// <summary>
        /// 手牌列表发生任何变化（抽牌、弃牌、合成、移除、出牌后）
        /// 参数1(int): 玩家ID
        /// 参数2(List&lt;CardEntity&gt;): 当前完整手牌列表（已排序）
        /// </summary>
        public Action<int, List<CardEntity>> OnHandCardsUpdated;
        #endregion

        #region 单卡操作事件
        /// <summary>
        /// 卡牌被抽出（从抽牌堆移入手牌）
         /// </summary>
        public Action<int, CardEntity> OnCardDrawn;

        /// <summary>
        /// 卡牌被弃置（从手牌/打出移入弃牌堆）
        /// </summary>
        public Action<int, CardEntity> OnCardDiscarded;

        /// <summary>
        /// 两张手牌合成
        /// 参数3(CardEntity): 被保留并升星的主卡
        /// 参数4(CardEntity): 被销毁的副卡
        /// 参数5(int): 合成后的星级
        /// </summary>
        public Action<int, CardEntity, CardEntity, int> OnCardMerged;

        /// <summary>
        /// 手牌位置交换（拖拽）
        /// </summary>
        public Action<int, int, int> OnHandCardSwapped; // playerId, fromIndex, toIndex

        /// <summary>
        /// 玩家打出了某张牌（已进入行动队列，但尚未结算）
        /// </summary>
        public Action<int, CardEntity, int> OnCardPlayed; // playerId, card, targetInstanceId
        #endregion

        #region 大招与角色事件
        /// <summary>
        /// 角色获得大招牌
        /// </summary>
        public Action<int, int, CardEntity> OnUltimateCardGranted; // playerId, characterConfigId, ultimateCard

        /// <summary>
        /// 角色死亡，其手牌/牌库中的卡被移除
        /// </summary>
        public Action<int, int, List<CardEntity>> OnCharacterCardsRemoved; // playerId, characterConfigId, removedCards
        #endregion

        #region 牌库事件
        /// <summary>
        /// 抽牌堆耗尽，弃牌堆重洗后形成新的抽牌堆
        /// </summary>
        public Action<int> OnDeckShuffled;
        #endregion

        #region 行动点事件
        /// <summary>
        /// 角色行动点变化
        /// 参数2(int): OwnerId（角色ConfigId）
        /// 参数3(int): 变化后的值
        /// </summary>
        public Action<int, int, int> OnActionPointChanged; // playerId, ownerId, currentValue
        #endregion
    }
}