/*
* ┌──────────────────────────────────┐
* │  描    述: 战斗流程事件播放器 + 角色同步 + 回合管理
* │  类    名: EventPlayer_CharacterShift.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Common.Defines;
using Data.card.Extensions;
using GameProtocol;
using Module.Character;
using Module.fight.CardMgr;
using UnityEngine;

namespace Module.fight.EventPlayer
{
    public static class EventPlayer_CharacterShift
    {
        public static async Task PlayBattleStart(BattleEvent evt)
        {
            SyncCharacters(evt.BattleStart.Entities);
#if UNITY_EDITOR
            Debug.Log($"[PlayEvent] 战斗开始，关卡: {evt.BattleStart.LevelId}");
#endif
            await Task.Delay(100);
        }

        public static async Task PlayBattleEnd(BattleEvent evt)
        {
            bool isWin = evt.BattleEnd.IsPlayerWin;
#if UNITY_EDITOR
            Debug.Log($"[PlayEvent] 战斗结束，玩家{(isWin ? "胜利" : "失败")}");
#endif
            GameApp.MessageCenter.PostEvent(EventDefines.OpenFightSettleView, isWin);
            await Task.Delay(100);
        }

        public static async Task PlayTurnStart(BattleEvent evt)
        {
            CompletePlayerTurnOutputIfNeeded();
#if UNITY_EDITOR
            Debug.Log($"[PlayEvent] 回合开始，轮数: {evt.TurnStart.RoundNumber}");
#endif
            GameApp.MessageCenter.PostEvent(EventDefines.OnBattleTurnStart, evt.TurnStart.IsPlayerTurn);

            if (evt.TurnStart.IsPlayerTurn)
            {
                PlayEventSequence.IsEnemyTurnResolving = false;
                GameApp.CardManager.CardActionQueue.Clear();
                GameApp.MessageCenter.PostEvent(EventDefines.OnPlayerTurnStart);
            }
            else
            {
                PlayEventSequence.IsEnemyTurnResolving = true;
            }

            await Task.Delay(300);
        }

        public static async Task PlayTurnEnd(BattleEvent evt)
        {
#if UNITY_EDITOR
            Debug.Log($"[PlayEvent] 回合结束，轮数: {evt.TurnEnd.RoundNumber}");
#endif
            GameApp.MessageCenter.PostEvent(EventDefines.OnBattleTurnEnd, evt.TurnEnd.IsPlayerTurn);

            if (evt.TurnEnd.IsPlayerTurn)
            {
                refreshAllHeroActionPointConstant();
                var actions = GameApp.CardManager.CardActionQueue.GetAction();
                PlayEventSequence.IsPlayerTurnResolving = true;
                PlayEventSequence.PendingPlayerPlayCardCount = 0;
                PlayEventSequence.PendingPlayerCasterOwnerIds.Clear();

                for (int i = 0; i < actions.Length; i++)
                {
                    if (actions[i].ActionType == CardActionType.PlayCard)
                    {
                        PlayEventSequence.PendingPlayerPlayCardCount++;
                        if (actions[i].cardEntity != null)
                        {
                            var cardConfig = actions[i].cardEntity.GetConfig();
                            if (cardConfig != null)
                                PlayEventSequence.PendingPlayerCasterOwnerIds.Enqueue(cardConfig.OwnerId);
                        }
                    }
                    else if (actions[i].ActionType == CardActionType.MoveCard)
                    {
                        GameApp.MessageCenter.PostEvent(EventDefines.OnMoveActionExecute, i);
                        await Task.Delay(350);
                    }
                }

                if (PlayEventSequence.PendingPlayerPlayCardCount == 0)
                    CompletePlayerTurnOutputIfNeeded();
            }
            else
            {
                PlayEventSequence.IsEnemyTurnResolving = false;
            }

            await Task.Delay(200);
        }

        public static void SyncCharacters(IList<CombatEntityInfo> entities)
        {
            if (entities == null || entities.Count == 0) return;

            var heroes = new List<BaseCharacter>(GameApp.EntityManager.GetAliveHeroes());
            var enemies = new List<BaseCharacter>(GameApp.EntityManager.GetAliveEnemies());
            var usedHeroes = new HashSet<BaseCharacter>();
            var usedEnemies = new HashSet<BaseCharacter>();

            for (int i = 0; i < entities.Count; i++)
            {
                CombatEntityInfo info = entities[i];
                var candidates = info.IsPlayerSide ? heroes : enemies;
                var used = info.IsPlayerSide ? usedHeroes : usedEnemies;
                BaseCharacter best = null;
                float bestHpDiff = float.MaxValue;

                for (int j = 0; j < candidates.Count; j++)
                {
                    BaseCharacter character = candidates[j];
                    if (used.Contains(character)) continue;
                    if (character.CharacterData == null || character.CharacterData.Id != info.ConfigId) continue;

                    float hpDiff = Mathf.Abs(character.CurrentHp - info.CurrentHp);
                    if (hpDiff < bestHpDiff)
                    {
                        bestHpDiff = hpDiff;
                        best = character;
                    }
                }

                if (best == null) continue;
                used.Add(best);
                best.SetCombatInstanceId(info.InstanceId);
                best.SetHpFromSnapshot(info.CurrentHp, info.MaxHp);

                if (best is HeroEntity hero)
                {
                    hero.SetActionPoint(info.ActionPoint);
                    hero.HUD?.UpdateActionPoint(info.ActionPoint, 0);
                }
            }
        }

        public static BaseCharacter FindCharacterByCombatInstanceId(int instanceId)
        {
            return GameApp.EntityManager.GetCharacterByCombatInstanceId(instanceId);
        }

        public static void PlayNextPlayerCasterAttack()
        {
            if (PlayEventSequence.PendingPlayerCasterOwnerIds.Count <= 0) return;

            int ownerId = PlayEventSequence.PendingPlayerCasterOwnerIds.Dequeue();
            BaseCharacter caster = GameApp.EntityManager.GetCharacterById(ownerId);
            if (caster == null || caster.CurrentStateType == CharacterStateType.Die) return;

            caster.ChangeState(CharacterStateType.Attack);
        }

        public static void CompletePlayerTurnOutputIfNeeded()
        {
            if (!PlayEventSequence.IsPlayerTurnResolving) return;

            PlayEventSequence.IsPlayerTurnResolving = false;
            PlayEventSequence.IsEnemyTurnResolving = false;
            PlayEventSequence.PendingPlayerPlayCardCount = 0;
            PlayEventSequence.PendingPlayerCasterOwnerIds.Clear();
            refreshAllHeroActionPointConstant();
            GameApp.MessageCenter.PostEvent(EventDefines.OnPlayerTurnOutput);
        }

        private static void refreshAllHeroActionPointConstant()
        {
            var heroes = GameApp.EntityManager.GetAliveHeroes();
            for (int i = 0; i < heroes.Count; i++)
            {
                HeroEntity hero = heroes[i];
                hero.HUD?.UpdateActionPoint(hero.ActionPoint, 0);
            }
        }
    }
}
