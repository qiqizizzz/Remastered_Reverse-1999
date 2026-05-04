/*
* ┌──────────────────────────────────┐
* │  描    述: 全局战斗状态机上下文容器                      
* │  类    名: CombatContext.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;

namespace Module.fight.Core.Models
{
    public class CombatContext
    {
        /// <summary>
        /// 全局回合数
        /// </summary>
        public int CurrentRound { get; set; }
        
        /// <summary>
        /// 当前玩家剩余总行动点数
        /// </summary>
        public int CurrentActionPoints { get; set; }
        
        /// <summary>
        /// 当前存活实体 (instanceId -> 实体数据)
        /// </summary>
        public Dictionary<string, EntityModel> Entities { get; set; }

        public CombatContext()
        {
            Entities = new Dictionary<string, EntityModel>();
            CurrentRound = 1;
            CurrentActionPoints = 4;
        }
        
        //TODO:后续待补充
    }
}