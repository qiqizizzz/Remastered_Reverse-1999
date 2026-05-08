/*
* ┌──────────────────────────────────┐
* │  描    述: 默认敌人AI，采用随机选牌与随机目标策略
* │  类    名: EnemyAI.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using System.Linq;
using GameServer.Battle.Core.Entities;
using GameServer.Battle.Data;
using GameServer.Battle.Data.Config;

namespace GameServer.Battle.AI
{
    internal class EnemyAI : IEnemyAI
    {
        private readonly CombatContext _context;
        private readonly ConfigManager _configManager;
        private readonly List<CombatEntity> _allEntities;
        private readonly Random _random;

        public EnemyAI(CombatContext context, ConfigManager configManager, List<CombatEntity> allEntities)
        {
            _context = context;
            _configManager = configManager;
            _allEntities = allEntities;
            _random = new Random();
        }

        // 生成敌人行动决策：随机选一张非大招牌，随机选一名存活玩家目标
        public EnemyDecision MakeDecision(CombatEntity enemy)
        {
            int cardId = selectRandomCard(enemy.ConfigId);
            if (cardId == 0) return null;

            int targetId = selectRandomTarget(enemy);

            return new EnemyDecision
            {
                CardConfigId = cardId,
                TargetInstanceId = targetId
            };
        }

        // 从敌人可用卡牌中随机选择一张非大招牌
        private int selectRandomCard(int enemyConfigId)
        {
            var enemyConfig = _configManager.GetCharacter(enemyConfigId);
            if (enemyConfig?.Cards == null || enemyConfig.Cards.Count == 0) return 0;

            var normalCards = new List<int>();
            foreach (var cardId in enemyConfig.Cards)
            {
                var cardConfig = _context.CardCatalog.Get(cardId);
                if (cardConfig != null && cardConfig.CardType != CardType.Ultimate)
                    normalCards.Add(cardId);
            }

            if (normalCards.Count == 0) return 0;

            return normalCards[_random.Next(normalCards.Count)];
        }

        // 随机选择一名存活的玩家方目标
        private int selectRandomTarget(CombatEntity enemy)
        {
            var targets = _allEntities
                .Where(e => e.OwnerPlayerId == 1 && e.CurrentHp > 0)
                .ToList();

            if (targets.Count == 0) return 0;

            return targets[_random.Next(targets.Count)].InstanceId;
        }
    }
}
