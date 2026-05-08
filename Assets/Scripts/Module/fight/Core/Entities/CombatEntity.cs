/*
* ┌──────────────────────────────────┐
* │  描    述: 战斗实体纯数据模型（前后端完全共用）                      
* │  类    名: CombatEntity.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Module.fight.Core.Entities
{
    public class CombatEntity
    {
        public int InstanceId { get; set; }
        public int ConfigId { get; set; }
        
        /// <summary>
        /// 归属玩家ID(1为自己,2为对手)
        /// </summary>
        public int OwnerPlayerId { get; set; }
        
        public float CurrentHp { get; set; }
        
        public int ActionPoint { get; set; }

        public CombatEntity(int instanceId, int configId, int ownerPlayerId, float currentHp, int actionPoint)
        {
            InstanceId = instanceId;
            ConfigId = configId;
            OwnerPlayerId = ownerPlayerId;
            CurrentHp = currentHp;
            ActionPoint = actionPoint;
        }
    }
}