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
using GameServer.Battle.Core.Extensions;
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
        private const int ENEMY_OWNER_START_ID = 2;
        private const int MAX_ACTION_POINT = 5;
        private const int INITIAL_HAND_COUNT = 8;

        // ==================== 依赖系统 ====================
        private readonly CombatContext _context;
        private readonly CardCombatSystem _cardSystem;
        private readonly CardSkillExecutor _skillExecutor;
        private readonly CombatCommandProcessor _commandProcessor;
        private readonly ConfigManager _configManager;
        private readonly IEnemyAI _enemyAI;
        private readonly BattleEventBuilder _eventBuilder;
        private readonly Random _random;

        // ==================== 状态字段 ====================
        private BattleState _state;
        private BattleResult _result;
        private int _levelId;
        private int _nextInstanceId;
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
            _nextInstanceId = 1;
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

            initBattle(heroConfigIds, monsterSpawns);
        }

        // ==================== 战斗初始化 ====================
        private void initBattle(List<int> heroConfigIds, List<MonsterSpawnData> monsterSpawns)
        {
            createHeroEntities(heroConfigIds);
            createEnemyEntities(monsterSpawns);
            initDecks(heroConfigIds);

            _state = BattleState.PlayerTurn;
        }

        // 创建玩家方英雄实体
        private void createHeroEntities(List<int> heroConfigIds)
        {
            foreach (var heroId in heroConfigIds)
            {
                var heroConfig = _configManager.GetCharacter(heroId);
                if (heroConfig == null) continue;

                var entity = new CombatEntity(
                    _nextInstanceId++,
                    heroId,
                    PLAYER_ID,
                    heroConfig.Property.Hp,
                    0
                );

                _context.Entities[heroId] = entity;
                _allEntities.Add(entity);
            }
        }

        // 创建敌方实体
        private void createEnemyEntities(List<MonsterSpawnData> monsterSpawns)
        {
            int enemyOwnerId = ENEMY_OWNER_START_ID;

            foreach (var spawn in monsterSpawns)
            {
                var enemyConfig = _configManager.GetCharacter(spawn.monsterId);
                if (enemyConfig == null) continue;

                for (int i = 0; i < spawn.count; i++)
                {
                    var entity = new CombatEntity(
                        _nextInstanceId++,
                        spawn.monsterId,
                        enemyOwnerId++,
                        enemyConfig.Property.Hp,
                        0
                    );

                    _allEntities.Add(entity);
                }
            }
        }

        // 初始化牌库与手牌
        private void initDecks(List<int> heroConfigIds)
        {
            _cardSystem.InitDeck(PLAYER_ID, heroConfigIds);
            _cardSystem.PrepareHandsForNewLevel(PLAYER_ID, heroConfigIds);
            processRoundStartHandFix();

            var battleStartEvent = new BattleEvent
            {
                EventType = BattleEventType.BattleStart,
                BattleStart = new BattleStartParams { LevelId = _levelId }
            };
            foreach (var entity in _allEntities)
            {
                battleStartEvent.BattleStart.Entities.Add(toProtoEntity(entity));
            }
            _eventBuilder.AddEvent(battleStartEvent);
            Console.WriteLine($"[initDecks] 牌库初始化完成, 英雄数: {heroConfigIds.Count}");
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

            resolvePlayerActions();
            if (_state == BattleState.BattleEnd) return;

            _eventBuilder.AddEvent(new BattleEvent
            {
                EventType = BattleEventType.TurnStart,
                TurnStart = new TurnStartParams { IsPlayerTurn = false, RoundNumber = _context.CurrentRound }
            });

            resolveEnemyTurn();
            if (_state == BattleState.BattleEnd) return;

            _eventBuilder.AddEvent(new BattleEvent
            {
                EventType = BattleEventType.TurnEnd,
                TurnEnd = new TurnEndParams { IsPlayerTurn = false, RoundNumber = _context.CurrentRound }
            });

            startNextRound();
        }

        // ==================== 回合结算 ====================
        // 结算玩家行动队列
        private void resolvePlayerActions()
        {
            var history = _commandProcessor.GetHistoryAndClear();
            int executeIndex = 0;

            foreach (var cmd in history)
            {
                if (!_isBattleActive()) break;
                if (cmd is not PlayCardCommand playCmd) continue;

                _eventBuilder.AddEvent(buildPlayerExecuteEvent(playCmd, executeIndex));
                executeIndex++;
                _skillExecutor.ExecuteCardEffect(playCmd.SenderPlayerId, playCmd.Card, playCmd.TargetInstanceId);
            }

            removeDeadEntities();

            if (tryEndBattle(out var result))
                endBattle(result);
        }

        // 构建玩家输出阶段的执行标记事件（用于客户端逐张驱动出牌动画）
        private static BattleEvent buildPlayerExecuteEvent(PlayCardCommand playCmd, int executeIndex)
        {
            return new BattleEvent
            {
                EventType = BattleEventType.EnqueueCard,
                TargetId = playCmd.TargetInstanceId,
                EnqueueCard = new EnqueueCardParams
                {
                    Card = toProtoCard(playCmd.Card),
                    QueueIndex = executeIndex,
                    ActionPointAfter = 0
                }
            };
        }

        // 结算敌人回合
        private void resolveEnemyTurn()
        {
            var enemies = getAliveEnemies();

            foreach (var enemy in enemies)
            {
                if (_state == BattleState.BattleEnd) break;
                if (getAliveHeroes().Count == 0) break;

                // TODO: Step 4 接入 EnemyAI，此处先使用临时随机选牌
                executeEnemyAction(enemy);
            }

            removeDeadEntities();

            if (tryEndBattle(out var result))
                endBattle(result);
        }

        // 敌人执行一次出牌
        private void executeEnemyAction(CombatEntity enemy)
        {
            var decision = _enemyAI.MakeDecision(enemy);
            if (decision == null) return;

            var card = new CardEntity(decision.CardConfigId);
            _eventBuilder.AddEvent(buildEnemyExecuteEvent(enemy, card, decision.TargetInstanceId));
            _skillExecutor.ExecuteCardEffect(enemy.OwnerPlayerId, card, decision.TargetInstanceId);
        }

        // 构建敌方输出阶段的执行标记事件（用于客户端按敌人逐个播放）
        private static BattleEvent buildEnemyExecuteEvent(CombatEntity enemy, CardEntity card, int targetInstanceId)
        {
            return new BattleEvent
            {
                EventType = BattleEventType.EnqueueCard,
                SourceId = enemy.InstanceId,
                TargetId = targetInstanceId,
                EnqueueCard = new EnqueueCardParams
                {
                    Card = toProtoCard(card),
                    QueueIndex = 0,
                    ActionPointAfter = 0
                }
            };
        }

        // 开始下一回合
        private void startNextRound()
        {
            _context.ActionQueue.QueuedCards.Clear();
            _context.CurrentRound++;

            processRoundStartHandFix();
            _state = BattleState.PlayerTurn;

            _eventBuilder.AddEvent(new BattleEvent
            {
                EventType = BattleEventType.TurnStart,
                TurnStart = new TurnStartParams { IsPlayerTurn = true, RoundNumber = _context.CurrentRound }
            });
            Console.WriteLine($"[startNextRound] 第 {_context.CurrentRound} 回合开始");
        }

        // 回合开始手牌修正（合成、补牌、发大招）
        private void processRoundStartHandFix()
        {
            int targetNormalCount = getTargetNormalHandCount();
            while (true)
            {
                while (_context.CheckAndAutoMerge(PLAYER_ID)) { }

                int normalCount = getNormalHandCardCount();
                Console.WriteLine($"[processRoundStartHandFix] 当前普通手牌数: {normalCount}, 目标: {targetNormalCount}");
                if (normalCount >= targetNormalCount) break;

                int needCount = targetNormalCount - normalCount;
                _cardSystem.DrawCard(PLAYER_ID, needCount);
            }

            foreach (var kvp in _context.Entities)
            {
                var entity = kvp.Value;
                if (entity.CurrentHp <= 0) continue;
                if (entity.ActionPoint >= MAX_ACTION_POINT)
                {
                    _cardSystem.TryGiveUltimateCard(PLAYER_ID, entity.ConfigId);
                }
            }
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

        // 战斗是否仍在进行
        private bool _isBattleActive()
        {
            return _state != BattleState.BattleEnd;
        }

        // 尝试判断战斗是否结束
        private bool tryEndBattle(out BattleResult result)
        {
            bool hasAliveHero = getAliveHeroes().Count > 0;
            bool hasAliveEnemy = getAliveEnemies().Count > 0;

            if (!hasAliveHero)
            {
                result = BattleResult.PlayerLose;
                return true;
            }

            if (!hasAliveEnemy)
            {
                result = BattleResult.PlayerWin;
                return true;
            }

            result = BattleResult.None;
            return false;
        }

        // 结束战斗
        private void endBattle(BattleResult result)
        {
            _state = BattleState.BattleEnd;
            _result = result;

            _eventBuilder.AddEvent(new BattleEvent
            {
                EventType = BattleEventType.BattleEnd,
                BattleEnd = new BattleEndParams { IsPlayerWin = result == BattleResult.PlayerWin }
            });
        }

        // 清理死亡实体及其卡牌
        private void removeDeadEntities()
        {
            var deadEntities = _allEntities.Where(e => e.CurrentHp <= 0).ToList();

            foreach (var dead in deadEntities)
            {
                _context.EventBus?.OnEntityDied?.Invoke(dead.InstanceId);

                if (_context.Entities.ContainsKey(dead.ConfigId))
                {
                    _cardSystem.RemoveCardsOfCharacter(PLAYER_ID, dead.ConfigId);
                    _context.Entities.Remove(dead.ConfigId);
                }
            }
        }

        // ==================== 工具函数 ====================
        // 获取手牌中非大招牌的数量
        private int getNormalHandCardCount()
        {
            if (!_context.PlayerDecks.TryGetValue(PLAYER_ID, out var deck)) return 0;

            int count = 0;
            foreach (var card in deck.HandCards)
            {
                var config = _context.CardCatalog.Get(card.ConfigId);
                if (config != null && config.CardType != CardType.Ultimate)
                    count++;
            }
            return count;
        }

        // 根据当前存活英雄数量计算回合补牌目标
        private int getTargetNormalHandCount()
        {
            int aliveHeroCount = getAliveHeroes().Count;
            return Math.Max(0, aliveHeroCount * 2);
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
            {
                snapshot.Entities.Add(toProtoEntity(entity));
            }

            if (_context.PlayerDecks.TryGetValue(PLAYER_ID, out var deck))
            {
                snapshot.PlayerDeck = new PlayerDeckInfo();
                foreach (var card in deck.HandCards)
                    snapshot.PlayerDeck.HandCards.Add(toProtoCard(card));
                foreach (var card in deck.DrawPile)
                    snapshot.PlayerDeck.DrawPile.Add(toProtoCard(card));
                foreach (var card in deck.DiscardPile)
                    snapshot.PlayerDeck.DiscardPile.Add(toProtoCard(card));
                snapshot.PlayerDeck.DrawPileCount = deck.DrawPile.Count;
            }

            snapshot.ActionQueue = new ActionQueueInfo();
            foreach (var card in _context.ActionQueue.QueuedCards)
                snapshot.ActionQueue.QueuedCards.Add(toProtoCard(card));
            snapshot.ActionQueue.MaxSize = _context.ActionQueue.MaxQueueSize;

            return snapshot;
        }

        // 将内部 CombatEntity 转为 Proto CombatEntityInfo
        private CombatEntityInfo toProtoEntity(CombatEntity entity)
        {
            var charConfig = _configManager.GetCharacter(entity.ConfigId);
            int maxHp = charConfig != null ? (int)charConfig.Property.Hp : 0;

            return new CombatEntityInfo
            {
                InstanceId = entity.InstanceId,
                ConfigId = entity.ConfigId,
                IsPlayerSide = entity.OwnerPlayerId == 1,
                CurrentHp = (int)entity.CurrentHp,
                MaxHp = maxHp,
                ActionPoint = entity.ActionPoint,
                MaxActionPoint = MAX_ACTION_POINT
            };
        }

        // 将内部 CardEntity 转为 Proto CardEntityInfo
        private static CardEntityInfo toProtoCard(CardEntity card)
        {
            return new CardEntityInfo
            {
                InstanceId = card.InstanceId,
                ConfigId = card.ConfigId,
                StarLevel = card.StarLevel
            };
        }
    }
}
