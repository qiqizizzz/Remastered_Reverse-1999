/*
* ┌───────────────────────────────────────────┐
* │  描    述: 卡牌技能执行器(用来计算最终数值等
* │  类    名: CardSkillExecutor.cs       
* │  创    建: By qiqizizzz
* └───────────────────────────────────────────┘
*/

using System.Collections.Generic;
using Data.card;
using Module.Character;
using Module.fight.CardMgr;
using UnityEngine;
using System.Threading.Tasks;
using Common.Defines;

namespace Module.fight.Skill
{
    public abstract class CardSkillExecutor
    {
        
        public static async Task ExecuteCardActionAsync(CardAction action)
        {
            CardData cardData = action.BattleCardData.BaseData;
            int starLevel = action.BattleCardData.StarLevel;

            BaseCharacter caster = GameApp.EntityManager.GetCharacterById(cardData.OwnerId);
            if (caster == null)
            {
                Debug.LogWarning($"未找到卡牌施放者，OwnerId: {cardData.OwnerId}");
                return;
            }
            
            Debug.Log($"[{caster.CharacterData.Name}] 使用了卡牌 [{cardData.Name}] (星级: {starLevel})");
            
            GameApp.MessageCenter.PostEvent(EventDefines.OnCardExecuteUI);

            await Task.Delay(300);
            
            caster.ChangeState(CharacterStateType.Attack);
            
            //TODO:特效等
            //施法前摇
            float attackDuration = caster.GetAnimDuration(caster.AnimConfig.AttackAnim);
            int preCastWaitMs = Mathf.RoundToInt(attackDuration * 0.5f * 1000);
            if (preCastWaitMs > 0) await Task.Delay(preCastWaitMs);
            
            if (cardData.Effects == null || cardData.Effects.Length == 0)
            {
                // [验证目标] 检查卡牌本身是否没有配置效果
                Debug.LogWarning($"卡牌 [{cardData.Name}] 没有配置任何 Effects！");
            }
            
            foreach (var effect in cardData.Effects)
            {
                List<BaseCharacter> targets = GetTargets(caster,effect, action.TargetInstanceId);

                switch (effect.EffectType)
                {
                    case EffectType.Damage:
                        ExecuteDamage(caster, targets, effect, starLevel);
                        break;
                    case EffectType.Heal:
                        break;
                    case EffectType.Buff:
                        break;
                    case EffectType.Debuff:
                        break;
                }
            }
            
            //施法后摇
            int postCastWaitMs = Mathf.RoundToInt(attackDuration * 0.5f * 1000);
            if (postCastWaitMs > 0) await Task.Delay(postCastWaitMs);
        }

        #region 卡牌效果执行
        private static void ExecuteDamage(BaseCharacter caster, List<BaseCharacter> targets, CardEffect effect, int starLevel)
        {
            Property casterProp = caster.CharacterData.Property;
            float starMultiplier = starLevel == 1 ? 1f : (starLevel == 2 ? 1.5f : 2f);
            
            //简单计算
            foreach (var target in targets)
            {
                Property targetProp = target.CharacterData.Property;
                float finalDamage = Mathf.Max((casterProp.Attack * effect.Value * starMultiplier) - targetProp.Defense,
                    1f);

                bool isCrit = Random.value < casterProp.CritRate;
                if (isCrit)
                {
                    finalDamage *= casterProp.CritDamage;
                    Debug.Log("暴击了！");
                }

                target.TakeDamage(Mathf.RoundToInt(finalDamage), isCrit);
                
                //TODO:伤害数字表现、特效等
            }
        }
        #endregion

        #region 获取目标对象
        public static List<BaseCharacter> GetTargets(BaseCharacter caster ,CardEffect effect, string targetInstanceId)
        {
            List<BaseCharacter> results = new List<BaseCharacter>();
            bool isCasterHero = caster is HeroEntity;

            List<BaseCharacter> pool = new List<BaseCharacter>();
            if (effect.Target == TargetType.Enemy)
            {
                //如果施放者是英雄，那么目标池就是敌人；如果施放者是敌人，那么目标池就是英雄
                if(isCasterHero)
                    pool.AddRange(GameApp.EntityManager.GetAliveEnemies());
                else
                    pool.AddRange(GameApp.EntityManager.GetAliveHeroes());
            }
            else
            {
                if (isCasterHero)
                    pool.AddRange(GameApp.EntityManager.GetAliveHeroes());
                else
                    pool.AddRange(GameApp.EntityManager.GetAliveEnemies());
            }

            //玩家出牌时优先选选中的目标
            if (isCasterHero && effect.Target == TargetType.Enemy && !string.IsNullOrEmpty(targetInstanceId))
            {
                BaseCharacter manualTarget = pool.Find(c => c.InstanceID == targetInstanceId);
                if (manualTarget != null && manualTarget.CurrentStateType != CharacterStateType.Die)
                {
                    results.Add(manualTarget);
                    if (effect.TargetCount == 1) return results;//如果只需要一个目标，直接返回
                }
            }

            //自动补齐目标
            foreach (var entity in pool)
            {
                if(entity.CurrentStateType == CharacterStateType.Die) continue;
                
                if(!results.Contains(entity)) results.Add(entity);
                
                if(effect.TargetCount > 0 && results.Count >= effect.TargetCount) break;
            }

            return results;
        }
        #endregion
    }
}