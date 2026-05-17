/*
* ┌──────────────────────────────────┐
* │  描    述: 战斗初始化系统，负责实体创建、牌堆初始化与新回合手牌修正（PvE + PvP）
* │  类    名: BattleInitSystem.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using GameProtocol;
using GameServer.Common;
using GameServer.Battle.Core.Entities;
using GameServer.Battle.Core.Extensions;
using GameServer.Battle.Data.Config;

namespace GameServer.Battle.Core.Systems
{
    internal class BattleInitSystem
    {
        #region 字段

        private const int ENEMY_OWNER_START_ID = 2;

        private readonly BattleEnv _env;
        private int _nextInstanceId;

        #endregion

        public BattleInitSystem(BattleEnv env)
        {
            _env = env;
        }

        #region 公共接口

        public void InitializePvE(int levelId, List<int> heroConfigIds, List<MonsterSpawnData> monsterSpawns)
        {
            _nextInstanceId = 1;

            createHeroEntities(heroConfigIds, _env.PlayerId);
            createEnemyEntities(monsterSpawns);
            initDeckForPlayer(_env.PlayerId, heroConfigIds, levelId);
        }

        public void InitializePvP(List<int> heroIdsP1, List<int> heroIdsP2)
        {
            _nextInstanceId = 1;

            createHeroEntities(heroIdsP1, _env.PlayerId);
            createHeroEntities(heroIdsP2, _env.Player2Id);
            initDeckForPlayer(_env.PlayerId, heroIdsP1);
            initDeckForPlayer(_env.Player2Id, heroIdsP2);
        }

        public void ProcessRoundStartHandFix()
        {
            int playerId = _env.CurrentPlayerId;
            int targetNormalCount = getTargetNormalHandCount(playerId);
            while (true)
            {
                while (_env.Context.CheckAndAutoMerge(playerId)) { }

                int normalCount = getNormalHandCardCount(playerId);
                int handTotal = normalCount + getUltimateHandCardCount(playerId);
                QLog.Info($"[processRoundStartHandFix] 玩家{playerId} 当前普通手牌数: {normalCount}, 目标: {targetNormalCount}, 手牌总数: {handTotal}");
                if (normalCount >= targetNormalCount) break;

                if (!_env.Context.PlayerDecks.TryGetValue(playerId, out var deck)) break;

                int needCount = targetNormalCount - normalCount;
                _env.CardSystem.DrawCard(playerId, needCount);
            }

            foreach (var kvp in _env.Context.Entities)
            {
                var entity = kvp.Value;
                if (entity.CurrentHp <= 0) continue;
                if (entity.ActionPoint >= _env.MaxActionPoint)
                {
                    if (_env.CardSystem.TryGiveUltimateCard(playerId, entity.ConfigId))
                    {
                        entity.ActionPoint = 0;
                        _env.Context.EventBus?.OnActionPointChanged?.Invoke(playerId, entity.ConfigId, 0);
                    }
                }
            }
        }

        public void UpdateScalingRules()
        {
            int aliveHeroCount = getAliveHeroCount(_env.CurrentPlayerId);
            int maxQueueSize = aliveHeroCount switch
            {
                4 => 4,
                3 => 3,
                2 => 2,
                1 => 2,
                _ => 4
            };
            _env.Context.ActionQueue.MaxQueueSize = maxQueueSize;
        }

        #endregion

        #region 实体创建与牌堆初始化

        private void createHeroEntities(List<int> heroConfigIds, int ownerPlayerId)
        {
            foreach (var heroId in heroConfigIds)
            {
                var heroConfig = _env.ConfigManager.GetCharacter(heroId);
                if (heroConfig == null) continue;

                var entity = new CombatEntity(
                    _nextInstanceId++,
                    heroId,
                    ownerPlayerId,
                    heroConfig.Property.Hp,
                    0
                );

                _env.Context.Entities[heroId] = entity;
                _env.AllEntities.Add(entity);
            }
        }

        private void createEnemyEntities(List<MonsterSpawnData> monsterSpawns)
        {
            int enemyOwnerId = ENEMY_OWNER_START_ID;

            foreach (var spawn in monsterSpawns)
            {
                var enemyConfig = _env.ConfigManager.GetCharacter(spawn.MonsterId);
                if (enemyConfig == null) continue;

                for (int i = 0; i < spawn.Count; i++)
                {
                    var entity = new CombatEntity(
                        _nextInstanceId++,
                        spawn.MonsterId,
                        enemyOwnerId++,
                        enemyConfig.Property.Hp,
                        0
                    );

                    _env.AllEntities.Add(entity);
                }
            }
        }

        private void initDeckForPlayer(int playerId, List<int> heroConfigIds, int levelId = 0)
        {
            _env.CardSystem.InitDeck(playerId, heroConfigIds);
            _env.CardSystem.PrepareHandsForNewLevel(playerId, heroConfigIds);
            UpdateScalingRules();

            int previousPlayerId = _env.CurrentPlayerId;
            _env.CurrentPlayerId = playerId;
            ProcessRoundStartHandFix();
            _env.CurrentPlayerId = previousPlayerId;

            if (_env.EventBuilder != null)
            {
                var battleStartEvent = new BattleEvent
                {
                    EventType = BattleEventType.BattleStart,
                    BattleStart = new BattleStartParams { LevelId = levelId }
                };
                if (_env.Mode == GameMode.PvE)
                {
                    foreach (var entity in _env.AllEntities)
                    {
                        battleStartEvent.BattleStart.Entities.Add(_env.ProtoSerializer.ToProtoEntity(entity));
                    }
                }
                _env.EventBuilder.AddEvent(battleStartEvent);
            }
            QLog.Info($"[initDecks] 玩家{playerId}牌库初始化完成, 英雄数: {heroConfigIds.Count}");
        }

        #endregion

        #region 工具函数

        private int getTargetNormalHandCount(int playerId)
        {
            int aliveHeroCount = getAliveHeroCount(playerId);
            return aliveHeroCount switch
            {
                4 => 8,
                3 => 6,
                2 => 6,
                1 => 4,
                _ => Math.Max(0, aliveHeroCount * 2)
            };
        }

        private int getNormalHandCardCount(int playerId)
        {
            if (!_env.Context.PlayerDecks.TryGetValue(playerId, out var deck)) return 0;

            int count = 0;
            foreach (var card in deck.HandCards)
            {
                var config = _env.Context.CardCatalog.Get(card.ConfigId);
                if (config != null && config.CardType != CardType.Ultimate)
                    count++;
            }
            return count;
        }

        private int getUltimateHandCardCount(int playerId)
        {
            if (!_env.Context.PlayerDecks.TryGetValue(playerId, out var deck)) return 0;

            int count = 0;
            foreach (var card in deck.HandCards)
            {
                var config = _env.Context.CardCatalog.Get(card.ConfigId);
                if (config != null && config.CardType == CardType.Ultimate)
                    count++;
            }
            return count;
        }

        private int getAliveHeroCount(int playerId)
        {
            int count = 0;
            foreach (var entity in _env.AllEntities)
            {
                if (entity.OwnerPlayerId == playerId && entity.CurrentHp > 0)
                    count++;
            }
            return count;
        }

        private int getAliveHeroCount()
        {
            return getAliveHeroCount(_env.PlayerId);
        }

        #endregion
    }
}
