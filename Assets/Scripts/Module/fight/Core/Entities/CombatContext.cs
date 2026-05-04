/*
* ┌──────────────────────────────────┐
* │  描    述: 全局战斗状态机上下文容器                      
* │  类    名: CombatContext.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;

namespace Module.fight.Core.Entities
{
    public class CombatContext
    {
        public int CurrentRound { get; set; }
        
        public int SequenceCardInstanceId { get; set; }
        
        public ActionQueueEntity ActionQueue { get; set; }
        public Dictionary<string, CombatEntity> Entities { get; set; }
        public Dictionary<int, PlayerDeckEntity> PlayerDecks { get; set; }

        public CombatContext()
        {
            ActionQueue = new ActionQueueEntity();
            Entities = new Dictionary<string, CombatEntity>();
            PlayerDecks = new Dictionary<int, PlayerDeckEntity>();
            
            CurrentRound = 1;
            SequenceCardInstanceId = 1000;
        }
        
        //TODO:后续待补充
    }
}