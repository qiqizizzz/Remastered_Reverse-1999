/*
* ┌──────────────────────────────────┐
* │  描    述: 敌人AI接口
* │  类    名: IEnemyAI.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using GameServer.Battle.Core.Entities;

namespace GameServer.Battle.AI
{
    internal interface IEnemyAI
    {
        // 为指定敌人生成一次行动决策
        EnemyDecision MakeDecision(CombatEntity enemy);
    }
}
