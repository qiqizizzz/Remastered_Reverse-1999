/*
* ┌──────────────────────────────────┐
* │  描    述: 玩家牌库数据模型                      
* │  类    名: PlayerDeckEntity.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Data.card;

namespace Module.fight.Core.Entities
{
    public class PlayerDeckEntity
    {
        public int PlayerId { get; set; }

        public List<CardEntity> DrawPile { get; set; } //抽牌堆
        public List<CardEntity> HandCards { get; set; } //手牌
        public List<CardEntity> DiscardPile { get; set; } //弃牌堆

        public PlayerDeckEntity()
        {
            DrawPile = new List<CardEntity>();
            HandCards = new List<CardEntity>();
            DiscardPile = new List<CardEntity>();
        }
    }
}