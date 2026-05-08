/*
* ┌──────────────────────────────────┐
* │  描    述: 敌人AI决策数据
* │  类    名: EnemyDecision.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace GameServer.Battle.AI
{
    // 敌人单次行动决策
    public class EnemyDecision
    {
        // 选择的卡牌配置Id
        public int CardConfigId { get; set; }
        
        // 选中的目标实例Id（0表示自动选择）
        public int TargetInstanceId { get; set; }
    }
}
