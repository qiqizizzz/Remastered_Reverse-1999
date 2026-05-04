/*
* ┌──────────────────────────────────┐
* │  描    述: 行动队列数据模型                      
* │  类    名: ActionQueueEntity.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Data.card;

namespace Module.fight.Core.Entities
{
    public class ActionQueueEntity
    {
        public List<CardEntity> QueuedCards { get; set; }
        public int MaxQueueSize { get; set; }
        
        public ActionQueueEntity()
        {
            QueuedCards = new List<CardEntity>();
            MaxQueueSize = 4; 
        }
    }
}