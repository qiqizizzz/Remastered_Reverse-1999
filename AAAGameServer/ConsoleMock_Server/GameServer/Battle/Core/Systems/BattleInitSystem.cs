/*
* ┌──────────────────────────────────┐
* │  描    述: 战斗初始化系统，负责实体创建、牌堆初始化与新回合手牌修正
* │  类    名: BattleInitSystem.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using GameProtocol;
using GameServer.Battle.Core.Entities;
using GameServer.Battle.Core.EventBus;
using GameServer.Battle.Core.Extensions;
using GameServer.Battle.Core.Serialization;
using GameServer.Battle.Data;
using GameServer.Battle.Data.Config;

namespace GameServer.Battle.Core.Systems
{
    internal class BattleInitSystem
    {
        private const int ENEMY_OWNER_START_ID = 2;

        private readonly ConfigManager _configManager;
        private readonly CombatContext _context;
        private readonly CardCombatSystem _cardSystem;
        private readonly BattleEventBuilder _eventBuilder;
        private readonly BattleProtoSerializer _protoSerializer;
        private readonly List<CombatEntity> _allEntities;
        private readonly int _playerId;
        private readonly int _maxActionPoint;

        private int _nextInstanceId;

        public BattleInitSystem(
            ConfigManager configManager,
            CombatContext context,
            CardCombatSystem cardSystem,
            BattleEventBuilder eventBuilder,
            BattleProtoSerializer protoSerializer,
            List<CombatEntity> allEntities,
            int playerId,
            int maxActionPoint)
        {
            _configManager = configManager;
            _context = context;
            _cardSystem = cardSystem;
            _eventBuilder = eventBuilder;
            _protoSerializer = protoSerializer;
            _allEntities = allEntities;
            _playerId = playerId;
            _maxActionPoint = maxActionPoint;
        }

        public void Initialize(int levelId, List<int> heroConfigIds, List<MonsterSpawnData> monsterSpawns)
        {
            _nextInstanceId = 1;

            createHeroEntities(heroConfigIds);
            createEnemyEntities(monsterSpawns);
            initDecks(levelId, heroConfigIds);
        }

        public void ProcessRoundStartHandFix()
        {
            int targetNormalCount = getTargetNormalHandCount();
            while (true)
            {
                while (_context.CheckAndAutoMerge(_playerId)) { }

                int normalCount = getNormalHandCardCount();
                int handTotal = normalCount + getUltimateHandCardCount();
                Console.WriteLine($"[processRoundStartHandFix] 当前普通手牌数: {normalCount}, 目标: {targetNormalCount}, 手牌总数: {handTotal}");
                if (normalCount >= targetNormalCount) break;

                if (!_context.PlayerDecks.TryGetValue(_playerId, out var deck)) break;
                if (deck.DrawPile.Count == 0 && deck.DiscardPile.Count == 0) break;

                int needCount = targetNormalCount - normalCount;
                _cardSystem.DrawCard(_playerId, needCount);
            }

            foreach (var kvp in _context.Entities)
            {
                var entity = kvp.Value;
                if (entity.CurrentHp <= 0) continue;
                if (entity.ActionPoint >= _maxActionPoint)
                {
                    if (_cardSystem.TryGiveUltimateCard(_playerId, entity.ConfigId))
                    {
                        entity.ActionPoint = 0;
                        _context.EventBus?.OnActionPointChanged?.Invoke(_playerId, entity.ConfigId, 0);
                    }
                }
            }
        }

        public void UpdateScalingRules()
        {
            int aliveHeroCount = getAliveHeroCount();
            int maxQueueSize = aliveHeroCount switch
            {
                4 => 4,
                3 => 3,
                2 => 2,
                1 => 2,
                _ => 4
            };
            _context.ActionQueue.MaxQueueSize = maxQueueSize;
        }

        #region 实体创建与牌堆初始化
        private void createHeroEntities(List<int> heroConfigIds)
        {
            foreach (var heroId in heroConfigIds)
            {
                var heroConfig = _configManager.GetCharacter(heroId);
                if (heroConfig == null) continue;

                var entity = new CombatEntity(
                    _nextInstanceId++,
                    heroId,
                    _playerId,
                    heroConfig.Property.Hp,
                    0
                );

                _context.Entities[heroId] = entity;
                _allEntities.Add(entity);
            }
        }

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

        private void initDecks(int levelId, List<int> heroConfigIds)
        {
            _cardSystem.InitDeck(_playerId, heroConfigIds);
            _cardSystem.PrepareHandsForNewLevel(_playerId, heroConfigIds);
            UpdateScalingRules();
            ProcessRoundStartHandFix();

            var battleStartEvent = new BattleEvent
            {
                EventType = BattleEventType.BattleStart,
                BattleStart = new BattleStartParams { LevelId = levelId }
            };
            foreach (var entity in _allEntities)
            {
                battleStartEvent.BattleStart.Entities.Add(_protoSerializer.ToProtoEntity(entity));
            }
            _eventBuilder.AddEvent(battleStartEvent);
            Console.WriteLine($"[initDecks] 牌库初始化完成, 英雄数: {heroConfigIds.Count}");
        }
        #endregion

        #region 工具函数
        private int getTargetNormalHandCount()
        {
            int aliveHeroCount = getAliveHeroCount();
            return aliveHeroCount switch
            {
                4 => 8,
                3 => 6,
                2 => 6,
                1 => 4,
                _ => Math.Max(0, aliveHeroCount * 2)
            };
        }

        private int getNormalHandCardCount()
        {
            if (!_context.PlayerDecks.TryGetValue(_playerId, out var deck)) return 0;

            int count = 0;
            foreach (var card in deck.HandCards)
            {
                var config = _context.CardCatalog.Get(card.ConfigId);
                if (config != null && config.CardType != CardType.Ultimate)
                    count++;
            }
            return count;
        }

        private int getUltimateHandCardCount()
        {
            if (!_context.PlayerDecks.TryGetValue(_playerId, out var deck)) return 0;

            int count = 0;
            foreach (var card in deck.HandCards)
            {
                var config = _context.CardCatalog.Get(card.ConfigId);
                if (config != null && config.CardType == CardType.Ultimate)
                    count++;
            }
            return count;
        }

        private int getAliveHeroCount()
        {
            int count = 0;
            foreach (var entity in _allEntities)
            {
                if (entity.OwnerPlayerId == _playerId && entity.CurrentHp > 0)
                    count++;
            }
            return count;
        }
        #endregion
    }
}
