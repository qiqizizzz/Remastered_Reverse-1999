/*
* ┌──────────────────────────────────┐
* │  描    述: 卡牌技能执行器，负责纯数值计算与效果应用
* │  类    名: CardSkillExecutor.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using GameServer.Battle.Core.Entities;
using GameServer.Battle.Core.EventBus;
using GameServer.Battle.Data;
using GameServer.Battle.Data.Config;

namespace GameServer.Battle.Core.Systems
{
    internal class CardSkillExecutor
    {
        private readonly CombatContext _context;
        private readonly ConfigManager _configManager;
        private readonly List<CombatEntity> _allEntities;
        private readonly Random _random;

        public CardSkillExecutor(CombatContext context, ConfigManager configManager, List<CombatEntity> allEntities)
        {
            _context = context;
            _configManager = configManager;
            _allEntities = allEntities;
            _random = new Random();
        }

        // 执行卡牌效果（同步计算，无表现层逻辑）
        public void ExecuteCardEffect(int playerId, CardEntity card, int targetInstanceId)
        {
            var cardConfig = _context.CardCatalog.Get(card.ConfigId);
            if (cardConfig?.Effects == null || cardConfig.Effects.Length == 0) return;

            var caster = resolveCaster(cardConfig.OwnerId);
            if (caster == null) return;

            foreach (var effect in cardConfig.Effects)
            {
                var targets = resolveTargets(caster, effect, targetInstanceId);

                switch (effect.EffectType)
                {
                    case EffectType.Damage:
                        applyDamage(caster, targets, effect, card.StarLevel);
                        break;
                    case EffectType.Heal:
                        applyHeal(caster, targets, effect, card.StarLevel);
                        break;
                    case EffectType.Buff:
                        // TODO: Buff 逻辑待实现
                        break;
                    case EffectType.Debuff:
                        // TODO: Debuff 逻辑待实现
                        break;
                }
            }
        }

        // 根据OwnerId（角色配置Id）查找施法者实体
        private CombatEntity resolveCaster(int ownerId)
        {
            _context.Entities.TryGetValue(ownerId, out var caster);
            return caster;
        }

        // 根据效果配置和手动选中的目标解析最终目标列表
        private List<CombatEntity> resolveTargets(CombatEntity caster, CardEffect effect, int manualTargetId)
        {
            var results = new List<CombatEntity>();
            bool isPlayerSide = caster.OwnerPlayerId == 1;
            var pool = buildTargetPool(isPlayerSide, effect.Target);

            // 玩家手动选中的目标优先
            if (isPlayerSide && effect.Target == TargetType.Enemy && manualTargetId != 0)
            {
                var manualTarget = pool.Find(c => c.InstanceId == manualTargetId);
                if (manualTarget != null && manualTarget.CurrentHp > 0)
                {
                    results.Add(manualTarget);
                    if (effect.TargetCount == 1) return results;
                }
            }

            // 自动补齐目标
            foreach (var entity in pool)
            {
                if (entity.CurrentHp <= 0) continue;
                if (!results.Contains(entity)) results.Add(entity);
                if (effect.TargetCount > 0 && results.Count >= effect.TargetCount) break;
            }

            return results;
        }

        // 构建候选目标池（根据阵营和效果目标类型筛选）
        private List<CombatEntity> buildTargetPool(bool casterIsPlayerSide, TargetType targetType)
        {
            var pool = new List<CombatEntity>();

            foreach (var entity in _allEntities)
            {
                if (entity.CurrentHp <= 0) continue;

                bool targetIsPlayerSide = entity.OwnerPlayerId == 1;
                bool isEnemy = casterIsPlayerSide != targetIsPlayerSide;

                if (targetType == TargetType.Enemy && isEnemy)
                    pool.Add(entity);
                else if (targetType == TargetType.Self && !isEnemy)
                    pool.Add(entity);
            }

            return pool;
        }

        // 对目标列表应用伤害效果
        private void applyDamage(CombatEntity caster, List<CombatEntity> targets, CardEffect effect, int starLevel)
        {
            var casterProp = _configManager.GetCharacter(caster.ConfigId)?.Property;
            if (casterProp == null) return;

            float starMultiplier = calculateStarMultiplier(starLevel);

            foreach (var target in targets)
            {
                var targetProp = _configManager.GetCharacter(target.ConfigId)?.Property;
                if (targetProp == null) continue;

                float rawDamage = casterProp.Attack * effect.Value * starMultiplier;
                float finalDamage = Math.Max(rawDamage - targetProp.Defense, 1f);

                bool isCrit = _random.NextDouble() < casterProp.CritRate;
                if (isCrit)
                    finalDamage *= casterProp.CritDamage;

                int damageValue = (int)Math.Round(finalDamage);
                target.CurrentHp = Math.Max(0, target.CurrentHp - damageValue);

                _context.EventBus?.OnDamageTaken?.Invoke(target.InstanceId, damageValue, isCrit);

                if (target.CurrentHp <= 0)
                    _context.EventBus?.OnEntityDied?.Invoke(target.InstanceId);
            }
        }

        // 对目标列表应用治疗效果
        private void applyHeal(CombatEntity caster, List<CombatEntity> targets, CardEffect effect, int starLevel)
        {
            var casterProp = _configManager.GetCharacter(caster.ConfigId)?.Property;
            if (casterProp == null) return;

            float starMultiplier = calculateStarMultiplier(starLevel);

            foreach (var target in targets)
            {
                var targetConfig = _configManager.GetCharacter(target.ConfigId);
                if (targetConfig?.Property == null) continue;

                float maxHp = targetConfig.Property.Hp;
                float healValue = casterProp.Attack * effect.Value * starMultiplier;

                int healAmount = (int)Math.Round(healValue);
                float beforeHp = target.CurrentHp;
                target.CurrentHp = Math.Min(maxHp, target.CurrentHp + healAmount);

                int actualHeal = (int)Math.Round(target.CurrentHp - beforeHp);

                _context.EventBus?.OnHealTaken?.Invoke(target.InstanceId, actualHeal);
            }
        }

        // 根据星级计算伤害/治疗倍率
        private static float calculateStarMultiplier(int starLevel)
        {
            return starLevel switch
            {
                2 => 1.5f,
                3 => 2f,
                _ => 1f
            };
        }
    }
}
