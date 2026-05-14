/*
* ┌──────────────────────────────────┐
* │  描    述: 伤害与治疗事件播放器
* │  类    名: EventPlayer_Damage.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Data.card;
using DG.Tweening;
using GameProtocol;
using Module.Character;
using UnityEngine;

namespace Module.fight.EventPlayer
{
    public static class EventPlayer_Damage
    {
        public static async Task PlayDamageTakenBatch(List<BattleEvent> events)
        {
            foreach (var evt in events)
            {
                var target = EventPlayer_CharacterShift.FindCharacterByCombatInstanceId(evt.TargetId);
                if (target != null)
                {
                    target.TakeDamage(evt.Damage.DamageValue, evt.Damage.IsCritical);
                    tryPlayCardVfx(evt.SourceCardConfigId, target);
                }
            }
            await Task.Delay(500);
        }

        public static async Task PlayDamageTaken(BattleEvent evt)
        {
            var target = EventPlayer_CharacterShift.FindCharacterByCombatInstanceId(evt.TargetId);
            if (target == null) return;

            target.TakeDamage(evt.Damage.DamageValue, evt.Damage.IsCritical);
            tryPlayCardVfx(evt.SourceCardConfigId, target);
            await Task.Delay(500);
        }

        public static async Task PlayHealTaken(BattleEvent evt)
        {
            var target = EventPlayer_CharacterShift.FindCharacterByCombatInstanceId(evt.TargetId);
            if (target == null) return;

            target.CurrentHp = Mathf.Min(target.MaxHp, target.CurrentHp + evt.Heal.HealValue);
            target.HUD?.UpdateHp(target.CurrentHp, target.MaxHp);
            tryPlayCardVfx(evt.SourceCardConfigId, target);
            await Task.Delay(300);
        }

        private static void tryPlayCardVfx(int configId, BaseCharacter target)
        {
            if (configId <= 0 || target == null) return;

            var cardData = GameApp.ConfigManager.Card.Get(configId);
            if (cardData != null && cardData.CardEffectPrefab != null)
            {
                GameObject prefab = cardData.CardEffectPrefab;
                Vector3 spawnPos = target.transform.position + Vector3.up * 1.25f;
                GameObject vfx = ResManager.InstantiateFromPool(prefab);
                if (vfx == null) return;

                vfx.transform.SetPositionAndRotation(spawnPos, Quaternion.identity);
                DOVirtual.DelayedCall(3f, () =>
                {
                    ResManager.ReleaseToPool(prefab, vfx);
                });
            }
        }
    }
}
