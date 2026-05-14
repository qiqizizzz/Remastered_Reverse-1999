/*
* ┌──────────────────────────────────┐
* │  描    述: 战斗实例，负责状态机与回合流转协调
* │  类    名: BattleInstance.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using System.Linq;
using GameProtocol;
using GameServer.Battle.AI;
using GameServer.Battle.Core.Commands;
using GameServer.Battle.Core.Entities;
using GameServer.Battle.Core.EventBus;
using GameServer.Battle.Core.Serialization;
using GameServer.Battle.Core.Systems;
using GameServer.Battle.Data;
using GameServer.Battle.Data.Config;

namespace GameServer.Battle
{
    public enum BattleState
    {
        Idle,
        PlayerTurn,
        Resolving,
        BattleEnd
    }

    public enum BattleResult
    {
        None,
        PlayerWin,
        PlayerLose
    }

    internal class BattleInstance
    {
        // ==================== 常量 ====================
        private const int PLAYER_ID = 1;
        private const int MAX_ACTION_POINT = 5;

        // ==================== 依赖系统 ====================
        private readonly CombatContext _context;
        private readonly CardCombatSystem _cardSystem;
        private readonly CardSkillExecutor _skillExecutor;
        private readonly CombatCommandProcessor _commandProcessor;
        private readonly ConfigManager _configManager;
        private readonly IEnemyAI _enemyAI;
        private readonly BattleEventBuilder _eventBuilder;
        private readonly BattleProtoSerializer _protoSerializer;
        private readonly BattleInitSystem _initSystem;
        private readonly BattleTurnSystem _turnSystem;
        private readonly Random _random;

        // ==================== 状态字段 ====================
        private BattleState _state;
        private BattleResult _result;
        private int _levelId;
        private readonly List<CombatEntity> _allEntities;

        // ==================== 属性 ====================
        public BattleState State => _state;
        public BattleResult Result => _result;
        public CombatContext Context => _context;
        public IReadOnlyList<CombatEntity> AllEntities => _allEntities;

        // 收集待发送的战斗事件
        public List<BattleEvent> CollectEvents()
        {
            return _eventBuilder.CollectAndClear();
        }

        public BattleInstance(
            int levelId,
            List<int> heroConfigIds,
            List<MonsterSpawnData> monsterSpawns,
            ConfigManager configManager,
            ICardCatalog cardCatalog)
        {
            _levelId = levelId;
            _configManager = configManager;
            _random = new Random();
            _state = BattleState.Idle;
            _result = BattleResult.None;
            _allEntities = new List<CombatEntity>();

            var eventBus = new CombatEventBus();
            _context = new CombatContext(cardCatalog, eventBus);
            _cardSystem = new CardCombatSystem(_context, cardCatalog, eventBus);
            _skillExecutor = new CardSkillExecutor(_context, configManager, _allEntities);
            _enemyAI = new EnemyAI(_context, configManager, _allEntities);
            _eventBuilder = new BattleEventBuilder(_context);
            _commandProcessor = new CombatCommandProcessor(_context, takeSnapshot, restoreSnapshot);
            _protoSerializer = new BattleProtoSerializer(configManager);

            _initSystem = new BattleInitSystem(
                configManager, _context, _cardSystem, _eventBuilder,
                _protoSerializer, _allEntities, PLAYER_ID, MAX_ACTION_POINT);

            _turnSystem = new BattleTurnSystem(
                _context, _commandProcessor, _skillExecutor, _enemyAI,
                _eventBuilder, _cardSystem, _initSystem,
                configManager, _protoSerializer, _allEntities, PLAYER_ID);

            _initSystem.Initialize(levelId, heroConfigIds, monsterSpawns);
            _state = BattleState.PlayerTurn;
        }

        // ==================== 公共操作接口 ====================
        // 玩家出牌
        public bool PlayCard(int cardInstanceId, int targetInstanceId, int handIndex)
        {
            if (_state != BattleState.PlayerTurn) return false;

            if (!_context.PlayerDecks.TryGetValue(PLAYER_ID, out var deck)) return false;

            if (_context.ActionQueue.QueuedCards.Count >= _context.ActionQueue.MaxQueueSize)
                return false;

            var card = deck.HandCards.Find(c => c.InstanceId == cardInstanceId);
            if (card == null) return false;

            var command = new PlayCardCommand(PLAYER_ID, card, targetInstanceId, handIndex);
            return _commandProcessor.Execute(command);
        }

        // 玩家交换手牌
        public bool MoveCard(int fromIndex, int toIndex)
        {
            if (_state != BattleState.PlayerTurn) return false;

            var command = new MoveCardCommand(PLAYER_ID, fromIndex, toIndex);
            return _commandProcessor.Execute(command);
        }

        // 撤销上一次操作
        public bool Undo()
        {
            if (_state != BattleState.PlayerTurn) return false;
            if (_commandProcessor.ActionCount == 0) return false;

            _commandProcessor.Undo();
            return true;
        }

        // 结束玩家回合，进入结算与敌人回合
        public void EndTurn()
        {
            if (_state != BattleState.PlayerTurn) return;

            _state = BattleState.Resolving;

            _eventBuilder.AddEvent(new BattleEvent
            {
                EventType = BattleEventType.TurnEnd,
                TurnEnd = new TurnEndParams { IsPlayerTurn = true, RoundNumber = _context.CurrentRound }
            });

            _turnSystem.ResolvePlayerActions();
            removeDeadEntities();
            if (tryEndBattle(out var endResult))
            {
                endBattle(endResult);
                return;
            }

            _eventBuilder.AddEvent(new BattleEvent
            {
                EventType = BattleEventType.TurnStart,
                TurnStart = new TurnStartParams { IsPlayerTurn = false, RoundNumber = _context.CurrentRound }
            });

            _turnSystem.ResolveEnemyTurn();
            removeDeadEntities();
            if (tryEndBattle(out var endResult2))
            {
                endBattle(endResult2);
                return;
            }

            _eventBuilder.AddEvent(new BattleEvent
            {
                EventType = BattleEventType.TurnEnd,
                TurnEnd = new TurnEndParams { IsPlayerTurn = false, RoundNumber = _context.CurrentRound }
            });

            _turnSystem.StartNextRound();
            _state = BattleState.PlayerTurn;
        }

        // ==================== 实体与胜负 ====================
        // 获取存活英雄
        private List<CombatEntity> getAliveHeroes()
        {
            return _allEntities.Where(e => e.OwnerPlayerId == PLAYER_ID && e.CurrentHp > 0).ToList();
        }

        // 获取存活敌人
        private List<CombatEntity> getAliveEnemies()
        {
            return _allEntities.Where(e => e.OwnerPlayerId != PLAYER_ID && e.CurrentHp > 0).ToList();
        }

        // 尝试判断战斗是否结束
        private bool tryEndBattle(out BattleResult res)
        {
            bool hasAliveHero = getAliveHeroes().Count > 0;
            bool hasAliveEnemy = getAliveEnemies().Count > 0;

            if (!hasAliveHero)
            {
                res = BattleResult.PlayerLose;
                return true;
            }

            if (!hasAliveEnemy)
            {
                res = BattleResult.PlayerWin;
                return true;
            }

            res = BattleResult.None;
            return false;
        }

        // 结束战斗
        private void endBattle(BattleResult res)
        {
            _state = BattleState.BattleEnd;
            _result = res;

            _eventBuilder.AddEvent(new BattleEvent
            {
                EventType = BattleEventType.BattleEnd,
                BattleEnd = new BattleEndParams { IsPlayerWin = res == BattleResult.PlayerWin }
            });
        }

        // 清理死亡实体及其卡牌
        private void removeDeadEntities()
        {
            var deadEntities = _allEntities.Where(e => e.CurrentHp <= 0).ToList();
            bool heroDied = false;

            foreach (var dead in deadEntities)
            {
                _context.EventBus?.OnEntityDied?.Invoke(dead.InstanceId);

                if (_context.Entities.ContainsKey(dead.ConfigId))
                {
                    _cardSystem.RemoveCardsOfCharacter(PLAYER_ID, dead.ConfigId);
                    _context.Entities.Remove(dead.ConfigId);
                    heroDied = true;
                }
            }

            if (heroDied)
                _initSystem.UpdateScalingRules();
        }

        // ==================== 快照与撤销 ====================
        // 记录当前状态快照
        private CardSnapshot takeSnapshot()
        {
            if (!_context.PlayerDecks.TryGetValue(PLAYER_ID, out var deck))
                return new CardSnapshot();

            var snapshot = new CardSnapshot
            {
                HeroActionPoints = _context.Entities.ToDictionary(e => e.Key, e => e.Value.ActionPoint),
                HandCards = new List<CardEntity>(deck.HandCards),
                DrawPile = new List<CardEntity>(deck.DrawPile),
                DiscardPile = new List<CardEntity>(deck.DiscardPile),
                CardStarLevels = new Dictionary<int, int>(),
                ActionQueue = new List<CardEntity>(_context.ActionQueue.QueuedCards)
            };

            foreach (var card in deck.HandCards) snapshot.CardStarLevels[card.InstanceId] = card.StarLevel;
            foreach (var card in deck.DrawPile) snapshot.CardStarLevels[card.InstanceId] = card.StarLevel;
            foreach (var card in deck.DiscardPile) snapshot.CardStarLevels[card.InstanceId] = card.StarLevel;

            return snapshot;
        }

        // 恢复快照
        private void restoreSnapshot(CardSnapshot snapshot)
        {
            if (snapshot == null) return;
            if (!_context.PlayerDecks.TryGetValue(PLAYER_ID, out var deck)) return;

            deck.HandCards = new List<CardEntity>(snapshot.HandCards);
            deck.DrawPile = new List<CardEntity>(snapshot.DrawPile);
            deck.DiscardPile = new List<CardEntity>(snapshot.DiscardPile);
            _context.ActionQueue.QueuedCards = snapshot.ActionQueue != null
                ? new List<CardEntity>(snapshot.ActionQueue)
                : new List<CardEntity>();

            foreach (var kvp in snapshot.CardStarLevels)
            {
                var card = findCardByInstanceId(kvp.Key);
                if (card != null) card.StarLevel = kvp.Value;
            }

            foreach (var kvp in snapshot.HeroActionPoints)
            {
                if (_context.Entities.TryGetValue(kvp.Key, out var entity))
                    entity.ActionPoint = kvp.Value;
            }

            _context.EventBus?.OnHandCardsUpdated?.Invoke(PLAYER_ID, new List<CardEntity>(deck.HandCards));
        }

        // 根据实例Id查找卡牌
        private CardEntity findCardByInstanceId(int instanceId)
        {
            if (!_context.PlayerDecks.TryGetValue(PLAYER_ID, out var deck)) return null;

            var all = new List<CardEntity>();
            all.AddRange(deck.HandCards);
            all.AddRange(deck.DrawPile);
            all.AddRange(deck.DiscardPile);
            return all.Find(c => c.InstanceId == instanceId);
        }

        // 获取完整战斗状态快照（用于断线重连）
        public BattleStateSnapshot GetStateSnapshot()
        {
            var snapshot = new BattleStateSnapshot
            {
                CurrentRound = _context.CurrentRound,
                IsPlayerTurn = _state == BattleState.PlayerTurn
            };

            foreach (var entity in _allEntities)
                snapshot.Entities.Add(_protoSerializer.ToProtoEntity(entity));

            if (_context.PlayerDecks.TryGetValue(PLAYER_ID, out var deck))
            {
                snapshot.PlayerDeck = new PlayerDeckInfo();
                foreach (var card in deck.HandCards)
                    snapshot.PlayerDeck.HandCards.Add(BattleProtoSerializer.ToProtoCard(card));
                foreach (var card in deck.DrawPile)
                    snapshot.PlayerDeck.DrawPile.Add(BattleProtoSerializer.ToProtoCard(card));
                foreach (var card in deck.DiscardPile)
                    snapshot.PlayerDeck.DiscardPile.Add(BattleProtoSerializer.ToProtoCard(card));
                snapshot.PlayerDeck.DrawPileCount = deck.DrawPile.Count;
            }

            snapshot.ActionQueue = new ActionQueueInfo();
            foreach (var card in _context.ActionQueue.QueuedCards)
                snapshot.ActionQueue.QueuedCards.Add(BattleProtoSerializer.ToProtoCard(card));
            snapshot.ActionQueue.MaxSize = _context.ActionQueue.MaxQueueSize;

            return snapshot;
        }
    }
}
