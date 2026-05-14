/*
* ┌──────────────────────────────────┐
* │  描    述: 回合结算系统，负责玩家/敌人行动执行与轮次推进
* │  类    名: BattleTurnSystem.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using GameProtocol;
using GameServer.Battle.AI;
using GameServer.Battle.Core.Commands;
using GameServer.Battle.Core.Entities;
using GameServer.Battle.Core.EventBus;
using GameServer.Battle.Core.Serialization;
using GameServer.Battle.Data;

namespace GameServer.Battle.Core.Systems
{
    internal class BattleTurnSystem
    {
        #region 字段

        private readonly BattleEnv _env;
        private readonly CombatCommandProcessor _commandProcessor;
        private readonly CardSkillExecutor _skillExecutor;
        private readonly IEnemyAI _enemyAI;
        private readonly BattleInitSystem _initSystem;

        #endregion

        public BattleTurnSystem(
            BattleEnv env,
            CombatCommandProcessor commandProcessor,
            CardSkillExecutor skillExecutor,
            IEnemyAI enemyAI,
            BattleInitSystem initSystem)
        {
            _env = env;
            _commandProcessor = commandProcessor;
            _skillExecutor = skillExecutor;
            _enemyAI = enemyAI;
            _initSystem = initSystem;
        }

        #region 公共接口

        public void ResolvePlayerActions()
        {
            var history = _commandProcessor.GetHistoryAndClear();
            int executeIndex = 0;

            foreach (var cmd in history)
            {
                if (cmd is not PlayCardCommand playCmd) continue;

                _env.EventBuilder.AddEvent(buildPlayerExecuteEvent(playCmd, executeIndex));
                executeIndex++;
                _skillExecutor.ExecuteCardEffect(playCmd.SenderPlayerId, playCmd.Card, playCmd.TargetInstanceId);
            }
        }

        public void ResolveEnemyTurn()
        {
            var enemies = new List<CombatEntity>();
            foreach (var entity in _env.AllEntities)
            {
                if (entity.OwnerPlayerId != _env.PlayerId && entity.CurrentHp > 0)
                    enemies.Add(entity);
            }

            foreach (var enemy in enemies)
            {
                if (!hasAliveHeroes()) break;

                executeEnemyAction(enemy);
            }
        }

        public void StartNextRound()
        {
            _env.Context.ActionQueue.QueuedCards.Clear();
            _env.Context.CurrentRound++;

            _initSystem.ProcessRoundStartHandFix();

            _env.EventBuilder.AddEvent(new BattleEvent
            {
                EventType = BattleEventType.TurnStart,
                TurnStart = new TurnStartParams { IsPlayerTurn = true, RoundNumber = _env.Context.CurrentRound }
            });
            Console.WriteLine($"[startNextRound] 第 {_env.Context.CurrentRound} 回合开始");
        }

        #endregion

        #region 事件构建

        private BattleEvent buildPlayerExecuteEvent(PlayCardCommand playCmd, int executeIndex)
        {
            return new BattleEvent
            {
                EventType = BattleEventType.EnqueueCard,
                TargetId = playCmd.TargetInstanceId,
                EnqueueCard = new EnqueueCardParams
                {
                    Card = BattleProtoSerializer.ToProtoCard(playCmd.Card),
                    QueueIndex = executeIndex,
                    ActionPointAfter = 0
                }
            };
        }

        private void executeEnemyAction(CombatEntity enemy)
        {
            var decision = _enemyAI.MakeDecision(enemy);
            if (decision == null) return;

            var card = new CardEntity(decision.CardConfigId);
            _env.EventBuilder.AddEvent(buildEnemyExecuteEvent(enemy, card, decision.TargetInstanceId));
            _skillExecutor.ExecuteCardEffect(enemy.OwnerPlayerId, card, decision.TargetInstanceId);
        }

        private BattleEvent buildEnemyExecuteEvent(CombatEntity enemy, CardEntity card, int targetInstanceId)
        {
            return new BattleEvent
            {
                EventType = BattleEventType.EnqueueCard,
                SourceId = enemy.InstanceId,
                TargetId = targetInstanceId,
                EnqueueCard = new EnqueueCardParams
                {
                    Card = BattleProtoSerializer.ToProtoCard(card),
                    QueueIndex = 0,
                    ActionPointAfter = 0
                }
            };
        }

        #endregion

        #region 工具函数

        private bool hasAliveHeroes()
        {
            foreach (var entity in _env.AllEntities)
            {
                if (entity.OwnerPlayerId == _env.PlayerId && entity.CurrentHp > 0)
                    return true;
            }
            return false;
        }

        #endregion
    }
}
