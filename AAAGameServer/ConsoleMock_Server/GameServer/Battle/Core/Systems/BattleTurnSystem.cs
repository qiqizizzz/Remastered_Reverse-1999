/*
* ┌──────────────────────────────────┐
* │  描    述: 回合结算系统，负责玩家/敌人行动执行与轮次推进
* │  类    名: BattleTurnSystem.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

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
        private readonly CombatContext _context;
        private readonly CombatCommandProcessor _commandProcessor;
        private readonly CardSkillExecutor _skillExecutor;
        private readonly IEnemyAI _enemyAI;
        private readonly BattleEventBuilder _eventBuilder;
        private readonly CardCombatSystem _cardSystem;
        private readonly BattleInitSystem _initSystem;
        private readonly ConfigManager _configManager;
        private readonly BattleProtoSerializer _protoSerializer;
        private readonly List<CombatEntity> _allEntities;
        private readonly int _playerId;

        public BattleTurnSystem(
            CombatContext context,
            CombatCommandProcessor commandProcessor,
            CardSkillExecutor skillExecutor,
            IEnemyAI enemyAI,
            BattleEventBuilder eventBuilder,
            CardCombatSystem cardSystem,
            BattleInitSystem initSystem,
            ConfigManager configManager,
            BattleProtoSerializer protoSerializer,
            List<CombatEntity> allEntities,
            int playerId)
        {
            _context = context;
            _commandProcessor = commandProcessor;
            _skillExecutor = skillExecutor;
            _enemyAI = enemyAI;
            _eventBuilder = eventBuilder;
            _cardSystem = cardSystem;
            _initSystem = initSystem;
            _configManager = configManager;
            _protoSerializer = protoSerializer;
            _allEntities = allEntities;
            _playerId = playerId;
        }

        public void ResolvePlayerActions()
        {
            var history = _commandProcessor.GetHistoryAndClear();
            int executeIndex = 0;

            foreach (var cmd in history)
            {
                if (cmd is not PlayCardCommand playCmd) continue;

                _eventBuilder.AddEvent(buildPlayerExecuteEvent(playCmd, executeIndex));
                executeIndex++;
                _skillExecutor.ExecuteCardEffect(playCmd.SenderPlayerId, playCmd.Card, playCmd.TargetInstanceId);
            }
        }

        public void ResolveEnemyTurn()
        {
            var enemies = new List<CombatEntity>();
            foreach (var entity in _allEntities)
            {
                if (entity.OwnerPlayerId != _playerId && entity.CurrentHp > 0)
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
            _context.ActionQueue.QueuedCards.Clear();
            _context.CurrentRound++;

            _initSystem.ProcessRoundStartHandFix();

            _eventBuilder.AddEvent(new BattleEvent
            {
                EventType = BattleEventType.TurnStart,
                TurnStart = new TurnStartParams { IsPlayerTurn = true, RoundNumber = _context.CurrentRound }
            });
            Console.WriteLine($"[startNextRound] 第 {_context.CurrentRound} 回合开始");
        }

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
            _eventBuilder.AddEvent(buildEnemyExecuteEvent(enemy, card, decision.TargetInstanceId));
            _skillExecutor.ExecuteCardEffect(enemy.OwnerPlayerId, card, decision.TargetInstanceId);
        }

        private static BattleEvent buildEnemyExecuteEvent(CombatEntity enemy, CardEntity card, int targetInstanceId)
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

        private bool hasAliveHeroes()
        {
            foreach (var entity in _allEntities)
            {
                if (entity.OwnerPlayerId == _playerId && entity.CurrentHp > 0)
                    return true;
            }
            return false;
        }
    }
}
