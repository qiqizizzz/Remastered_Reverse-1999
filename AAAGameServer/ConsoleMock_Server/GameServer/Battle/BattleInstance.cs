/*
* ┌──────────────────────────────────┐
* │  描    述: 战斗实例，负责状态机与回合流转协调（PvE + PvP）
* │  类    名: BattleInstance.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using System.Linq;
using GameProtocol;
using GameServer.Battle.AI;
using GameServer.Battle.Core;
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
        #region 字段

        // ==================== 常量 ====================
        private const int PLAYER1_ID = 1;
        private const int PLAYER2_ID = 2;
        private const int MAX_ACTION_POINT = 5;

        // ==================== 运行环境 ====================
        private readonly BattleEnv _env;
        private readonly Random _random;

        // ==================== 子系统 ====================
        private readonly CombatCommandProcessor _commandProcessor;
        private readonly CardSkillExecutor _skillExecutor;
        private readonly IEnemyAI _enemyAI;
        private readonly BattleInitSystem _initSystem;
        private readonly BattleTurnSystem _turnSystem;

        // ==================== 状态字段 ====================
        private BattleState _state;
        private BattleResult _result;
        private int _levelId;

        #endregion

        #region 属性

        public BattleState State => _state;
        public BattleResult Result => _result;
        public CombatContext Context => _env.Context;
        public IReadOnlyList<CombatEntity> AllEntities => _env.AllEntities;
        public GameMode Mode => _env.Mode;

        #endregion

        public List<BattleEvent> CollectEvents()
        {
            return _env.EventBuilder.CollectAndClear();
        }

        #region 构造函数

        public BattleInstance(
            int levelId,
            List<int> heroIdsP1,
            List<MonsterSpawnData> monsterSpawns,
            ConfigManager configManager,
            ICardCatalog cardCatalog)
            : this(levelId, heroIdsP1, null, monsterSpawns, configManager, cardCatalog, GameMode.PvE)
        {
        }

        internal BattleInstance(
            int levelId,
            List<int> heroIdsP1,
            List<int> heroIdsP2,
            List<MonsterSpawnData> monsterSpawns,
            ConfigManager configManager,
            ICardCatalog cardCatalog,
            GameMode gameMode)
        {
            _levelId = levelId;
            _random = new Random();
            _state = BattleState.Idle;
            _result = BattleResult.None;

            var allEntities = new List<CombatEntity>();
            var eventBus = new CombatEventBus();
            var context = new CombatContext(cardCatalog, eventBus);
            var cardSystem = new CardCombatSystem(context, cardCatalog, eventBus);
            var eventBuilder = new BattleEventBuilder(context);
            var protoSerializer = new BattleProtoSerializer(configManager);

            _env = new BattleEnv(
                configManager, context, cardSystem, eventBuilder,
                protoSerializer, allEntities,
                PLAYER1_ID, PLAYER2_ID, MAX_ACTION_POINT, gameMode);

            _skillExecutor = new CardSkillExecutor(context, configManager, allEntities);
            _enemyAI = new EnemyAI(context, configManager, allEntities);
            _commandProcessor = new CombatCommandProcessor(context, takeSnapshot, restoreSnapshot);

            _initSystem = new BattleInitSystem(_env);
            _turnSystem = new BattleTurnSystem(_env, _commandProcessor, _skillExecutor, _enemyAI, _initSystem);

            if (gameMode == GameMode.PvE)
            {
                _initSystem.InitializePvE(levelId, heroIdsP1, monsterSpawns ?? new List<MonsterSpawnData>());
            }
            else
            {
                _initSystem.InitializePvP(heroIdsP1, heroIdsP2 ?? new List<int>());
                _env.CurrentPlayerId = PLAYER2_ID;
                addPvpTurnStartEvent();
            }

            _state = BattleState.PlayerTurn;
        }

        #endregion

        #region 公共操作接口

        public bool PlayCard(int playerId, int cardInstanceId, int targetInstanceId, int handIndex)
        {
            if (_state != BattleState.PlayerTurn) return false;
            if (!isCurrentPlayer(playerId)) return false;
            if (!_env.Context.PlayerDecks.TryGetValue(playerId, out var deck)) return false;

            if (_env.Context.ActionQueue.QueuedCards.Count >= _env.Context.ActionQueue.MaxQueueSize)
                return false;

            var card = deck.HandCards.Find(c => c.InstanceId == cardInstanceId);
            if (card == null) return false;

            var command = new PlayCardCommand(playerId, card, targetInstanceId, handIndex);
            return _commandProcessor.Execute(command);
        }

        public bool MoveCard(int playerId, int fromIndex, int toIndex)
        {
            if (_state != BattleState.PlayerTurn) return false;
            if (!isCurrentPlayer(playerId)) return false;

            var command = new MoveCardCommand(playerId, fromIndex, toIndex);
            return _commandProcessor.Execute(command);
        }

        public bool Undo(int playerId)
        {
            if (_state != BattleState.PlayerTurn) return false;
            if (!isCurrentPlayer(playerId)) return false;
            if (_commandProcessor.ActionCount == 0) return false;

            _commandProcessor.Undo();
            return true;
        }

        public void EndTurn(int playerId)
        {
            if (_state != BattleState.PlayerTurn) return;
            if (!isCurrentPlayer(playerId)) return;

            _state = BattleState.Resolving;

            _env.EventBuilder.AddEvent(new BattleEvent
            {
                EventType = BattleEventType.TurnEnd,
                EventOwnerId = playerId,
                TurnEnd = new TurnEndParams { IsPlayerTurn = true, RoundNumber = _env.Context.CurrentRound }
            });

            _turnSystem.ResolvePlayerActions();

            if (_env.Mode == GameMode.PvE)
            {
                endTurnPvE();
            }
            else
            {
                endTurnPvP();
            }
        }

        private void endTurnPvE()
        {
            removeDeadEntities();
            if (tryEndBattlePvE(out var r))
            {
                endBattle(r);
                return;
            }

            _env.EventBuilder.AddEvent(new BattleEvent
            {
                EventType = BattleEventType.TurnStart,
                TurnStart = new TurnStartParams { IsPlayerTurn = false, RoundNumber = _env.Context.CurrentRound }
            });

            _turnSystem.ResolveEnemyTurn();
            removeDeadEntities();
            if (tryEndBattlePvE(out var r2))
            {
                endBattle(r2);
                return;
            }

            _env.EventBuilder.AddEvent(new BattleEvent
            {
                EventType = BattleEventType.TurnEnd,
                TurnEnd = new TurnEndParams { IsPlayerTurn = false, RoundNumber = _env.Context.CurrentRound }
            });

            _turnSystem.StartNextRound();
            _state = BattleState.PlayerTurn;
        }

        private void endTurnPvP()
        {
            removeDeadEntities();
            if (tryEndBattlePvP(out var r))
            {
                endBattle(r);
                return;
            }

            _env.Context.ActionQueue.QueuedCards.Clear();

            int previousPlayerId = _env.CurrentPlayerId;
            _env.CurrentPlayerId = getOpponentPlayerId(previousPlayerId);

            if (previousPlayerId == PLAYER1_ID)
                _turnSystem.StartNextRound();
            else
                addPvpTurnStartEvent();

            _state = BattleState.PlayerTurn;
        }

        #endregion

        #region 实体与胜负

        private bool isCurrentPlayer(int playerId)
        {
            return _env.Mode == GameMode.PvP
                ? playerId == _env.CurrentPlayerId
                : playerId == 0 || playerId == PLAYER1_ID;
        }

        // 获取PVP当前玩家的对手玩家Id
        private static int getOpponentPlayerId(int playerId)
        {
            return playerId == PLAYER1_ID ? PLAYER2_ID : PLAYER1_ID;
        }

        // 添加PVP玩家回合开始事件
        private void addPvpTurnStartEvent()
        {
            _env.EventBuilder.AddEvent(new BattleEvent
            {
                EventType = BattleEventType.TurnStart,
                EventOwnerId = _env.CurrentPlayerId,
                TurnStart = new TurnStartParams { IsPlayerTurn = true, RoundNumber = _env.Context.CurrentRound }
            });
        }

        private List<CombatEntity> getAliveHeroes()
        {
            return _env.AllEntities.Where(e => e.OwnerPlayerId == PLAYER1_ID && e.CurrentHp > 0).ToList();
        }

        private List<CombatEntity> getAliveEnemies()
        {
            return _env.AllEntities.Where(e => e.OwnerPlayerId != PLAYER1_ID && e.CurrentHp > 0).ToList();
        }

        private bool tryEndBattlePvE(out BattleResult res)
        {
            if (getAliveHeroes().Count == 0)
            {
                res = BattleResult.PlayerLose;
                return true;
            }
            if (getAliveEnemies().Count == 0)
            {
                res = BattleResult.PlayerWin;
                return true;
            }
            res = BattleResult.None;
            return false;
        }

        private bool tryEndBattlePvP(out BattleResult res)
        {
            bool p1Alive = _env.AllEntities.Any(e => e.OwnerPlayerId == PLAYER1_ID && e.CurrentHp > 0);
            bool p2Alive = _env.AllEntities.Any(e => e.OwnerPlayerId == PLAYER2_ID && e.CurrentHp > 0);

            if (!p1Alive && !p2Alive)
            {
                res = BattleResult.None; // 平局
                return true;
            }
            if (!p1Alive)
            {
                res = BattleResult.PlayerLose;
                return true;
            }
            if (!p2Alive)
            {
                res = BattleResult.PlayerWin;
                return true;
            }
            res = BattleResult.None;
            return false;
        }

        private void endBattle(BattleResult res)
        {
            _state = BattleState.BattleEnd;
            _result = res;

            _env.EventBuilder.AddEvent(new BattleEvent
            {
                EventType = BattleEventType.BattleEnd,
                BattleEnd = new BattleEndParams { IsPlayerWin = res == BattleResult.PlayerWin }
            });
        }

        private void removeDeadEntities()
        {
            var deadEntities = _env.AllEntities.Where(e => e.CurrentHp <= 0).ToList();
            bool heroDied = false;

            foreach (var dead in deadEntities)
            {
                _env.Context.EventBus?.OnEntityDied?.Invoke(dead.InstanceId);

                if (_env.Context.Entities.ContainsKey(dead.ConfigId))
                {
                    _env.CardSystem.RemoveCardsOfCharacter(PLAYER1_ID, dead.ConfigId);
                    _env.CardSystem.RemoveCardsOfCharacter(PLAYER2_ID, dead.ConfigId);
                    _env.Context.Entities.Remove(dead.ConfigId);
                    heroDied = true;
                }
            }

            if (heroDied)
                _initSystem.UpdateScalingRules();
        }

        #endregion

        #region 快照与撤销

        private CardSnapshot takeSnapshot()
        {
            int playerId = _env.CurrentPlayerId;
            if (!_env.Context.PlayerDecks.TryGetValue(playerId, out var deck))
                return new CardSnapshot();

            var snapshot = new CardSnapshot
            {
                HeroActionPoints = _env.Context.Entities.ToDictionary(e => e.Key, e => e.Value.ActionPoint),
                HandCards = new List<CardEntity>(deck.HandCards),
                DrawPile = new List<CardEntity>(deck.DrawPile),
                DiscardPile = new List<CardEntity>(deck.DiscardPile),
                CardStarLevels = new Dictionary<int, int>(),
                ActionQueue = new List<CardEntity>(_env.Context.ActionQueue.QueuedCards)
            };

            foreach (var card in deck.HandCards) snapshot.CardStarLevels[card.InstanceId] = card.StarLevel;
            foreach (var card in deck.DrawPile) snapshot.CardStarLevels[card.InstanceId] = card.StarLevel;
            foreach (var card in deck.DiscardPile) snapshot.CardStarLevels[card.InstanceId] = card.StarLevel;

            return snapshot;
        }

        private void restoreSnapshot(CardSnapshot snapshot)
        {
            if (snapshot == null) return;
            int playerId = _env.CurrentPlayerId;
            if (!_env.Context.PlayerDecks.TryGetValue(playerId, out var deck)) return;

            deck.HandCards = new List<CardEntity>(snapshot.HandCards);
            deck.DrawPile = new List<CardEntity>(snapshot.DrawPile);
            deck.DiscardPile = new List<CardEntity>(snapshot.DiscardPile);
            _env.Context.ActionQueue.QueuedCards = snapshot.ActionQueue != null
                ? new List<CardEntity>(snapshot.ActionQueue)
                : new List<CardEntity>();

            foreach (var kvp in snapshot.CardStarLevels)
            {
                var card = findCardByInstanceId(playerId, kvp.Key);
                if (card != null) card.StarLevel = kvp.Value;
            }

            foreach (var kvp in snapshot.HeroActionPoints)
            {
                if (_env.Context.Entities.TryGetValue(kvp.Key, out var entity))
                    entity.ActionPoint = kvp.Value;
            }

            _env.Context.EventBus?.OnHandCardsUpdated?.Invoke(playerId, new List<CardEntity>(deck.HandCards));
        }

        private CardEntity findCardByInstanceId(int playerId, int instanceId)
        {
            if (!_env.Context.PlayerDecks.TryGetValue(playerId, out var deck)) return null;

            var all = new List<CardEntity>();
            all.AddRange(deck.HandCards);
            all.AddRange(deck.DrawPile);
            all.AddRange(deck.DiscardPile);
            return all.Find(c => c.InstanceId == instanceId);
        }

        public BattleStateSnapshot GetStateSnapshot(int viewerPlayerId = PLAYER1_ID)
        {
            var snapshot = new BattleStateSnapshot
            {
                CurrentRound = _env.Context.CurrentRound,
                IsPlayerTurn = _state == BattleState.PlayerTurn
            };

            foreach (var entity in _env.AllEntities)
                snapshot.Entities.Add(_env.ProtoSerializer.ToProtoEntity(entity, viewerPlayerId));

            if (_env.Context.PlayerDecks.TryGetValue(viewerPlayerId, out var deck))
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
            foreach (var card in _env.Context.ActionQueue.QueuedCards)
                snapshot.ActionQueue.QueuedCards.Add(BattleProtoSerializer.ToProtoCard(card));
            snapshot.ActionQueue.MaxSize = _env.Context.ActionQueue.MaxQueueSize;

            return snapshot;
        }

        #endregion
    }
}
