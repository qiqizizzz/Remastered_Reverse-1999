/*
* ┌──────────────────────────────────┐
* │  描    述: 角色死亡与行动点变化事件播放器
* │  类    名: EventPlayer_CharacterDie.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Threading.Tasks;
using Common;
using Common.Defines;
using Data.card;
using Data.card.Extensions;
using GameProtocol;
using Module.Character;
using Module.fight.CardMgr;
using Module.fight.Core.Entities;
using UnityEngine;

namespace Module.fight.EventPlayer
{
    public static class EventPlayer_CharacterDie
    {
        public static async Task PlayEntityDied(BattleEvent evt)
        {
            var target = EventPlayer_CharacterShift.FindCharacterByCombatInstanceId(evt.TargetId);
            if (target == null) return;

            if (target.CurrentStateType != CharacterStateType.Die)
                target.ChangeState(CharacterStateType.Die);

            GameApp.MessageCenter.PostEvent(EventDefines.OnCharacterDie, target);
            await Task.Delay(500);
        }

        public static async Task PlayActionPointChanged(BattleEvent evt)
        {
            var entityInfo = evt.ActionPointChanged;
            var hero = GameApp.EntityManager.GetCharacterById(entityInfo.EntityId) as HeroEntity;
            if (hero == null) return;

            hero.SetActionPoint(entityInfo.NewValue);

            if (GameApp.CardManager.BattleContext.Entities.TryGetValue(entityInfo.EntityId, out var contextEntity))
                contextEntity.ActionPoint = entityInfo.NewValue;

            if (PlayEventSequence.IsPlayerTurnResolving || PlayEventSequence.IsEnemyTurnResolving)
            {
                hero.HUD?.UpdateActionPoint(entityInfo.NewValue, 0);
            }
            else
            {
                int previewGain = 0;
                CardAction[] queuedActions = GameApp.CardManager.CardActionQueue.GetAction();
                for (int i = 0; i < queuedActions.Length; i++)
                {
                    CardAction action = queuedActions[i];
                    if (action.ActionType != CardActionType.PlayCard || action.cardEntity == null) continue;
                    var config = action.cardEntity.GetConfig();
                    if (config == null || config.CardType == CardType.Ultimate) continue;
                    if (config.OwnerId == hero.CharacterData.Id) previewGain++;
                }

                int confirmedActionPoint = Mathf.Max(0, entityInfo.NewValue - previewGain);
                hero.HUD?.UpdateActionPoint(confirmedActionPoint, previewGain);
            }
            await Task.Delay(100);
        }
    }
}
