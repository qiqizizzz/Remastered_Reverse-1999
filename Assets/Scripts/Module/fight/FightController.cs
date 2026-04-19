/*
* ┌──────────────────────────────────────────────────────────────────────────────────────────────────────┐
* │  描    述: 战斗控制器(只负责管理宏观的战斗流程(控制回合)，包括回合开始、回合结束、战斗结束等，不涉及具体的战斗逻辑)
* │  类    名: FightController.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────────────────────────────────────────────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Common.Defines;
using Data.card;
using Data.level;
using Module.Character;
using Module.fight.CardMgr;
using Module.fight.Component;
using Module.fight.Skill;
using MVC;
using MVC.Controller;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Module.fight
{
    public class FightController : BaseController
    {
        private LevelInitData _currentInitData;
        
        public FightController() : base()
        {
            GameApp.ViewManager.Register(ViewType.FightingView, new ViewInfo()
            {
                PrefabName = AddressDefines.UI_FightingView,
                controller = this,
                parentTf = GameApp.ViewManager.canvasTf
            });
            
            GameApp.ViewManager.Register(ViewType.PauseFightView, new ViewInfo()
            {
                PrefabName =  AddressDefines.UI_PauseFightView,
                controller = this,
                parentTf = GameApp.ViewManager.canvasTf,
                Sorting_Order = 10
            });
            
            InitModuleEvent();
        }

        public override void InitModuleEvent()
        {
            RegisterFunc(EventDefines.OpenFightingView, onOpenFightView);
            RegisterFunc(EventDefines.OpenPauseFightView, onOpenPauseFightView);
            RegisterFunc(EventDefines.FightingViewReady,onFightingViewReady);
            
            GameApp.MessageCenter.AddEvent(EventDefines.OnPlayerTurnStart, onPlayerTurnStart);
            GameApp.MessageCenter.AddEvent(EventDefines.OnPlayerTurnOutput, onPlayerTurnOutput);
            GameApp.MessageCenter.AddEvent(EventDefines.OnEnemyTurn, onEnemyTurn);
            GameApp.MessageCenter.AddEvent(EventDefines.OnSelectEnemyTarget, onSelectEnemyTarget);
            GameApp.MessageCenter.AddEvent(EventDefines.OnCharacterDie, onCharacterDie);
        }

        public override void RemoveModuleEvent()
        {
            UnRegisterFunc(EventDefines.OpenFightingView, onOpenFightView);
            UnRegisterFunc(EventDefines.OpenPauseFightView, onOpenPauseFightView);
            UnRegisterFunc(EventDefines.FightingViewReady,onFightingViewReady);
            
            GameApp.MessageCenter.RemoveEvent(EventDefines.OnPlayerTurnStart, onPlayerTurnStart);
            GameApp.MessageCenter.RemoveEvent(EventDefines.OnPlayerTurnOutput, onPlayerTurnOutput);
            GameApp.MessageCenter.RemoveEvent(EventDefines.OnEnemyTurn, onEnemyTurn);
            GameApp.MessageCenter.RemoveEvent(EventDefines.OnSelectEnemyTarget, onSelectEnemyTarget);
            GameApp.MessageCenter.RemoveEvent(EventDefines.OnCharacterDie, onCharacterDie);
        }

        #region UI事件
        private void onOpenFightView(System.Object[] args)
        {
            _currentInitData = args[0] as LevelInitData;
            
            GameApp.EntityManager.SpawnBattleEntities(_currentInitData, onSpawnBattleCallback);//生成玩家与敌人等
        }

        private void onOpenPauseFightView(System.Object[] args)
        {
            GameApp.ViewManager.Open(ViewType.PauseFightView);
        }
        
        private void onFightingViewReady(System.Object[] args)
        {
            StartFirstRound();
        }
        #endregion

        #region 战斗事件
        private void onPlayerTurnStart(System.Object args)
        {
            Debug.Log("==== 玩家回合开始 ====");
            
            //TODO:...
        }
        
        private async void onPlayerTurnOutput(System.Object args)
        {
            try
            {
                Debug.Log("==== 玩家出牌阶段结束，开始结算队列 ====");
                //TODO:禁用UI交互,播放出牌动画等...
                List<CardAction> actions = GameApp.CardManager.CardActionQueue.GetAllActionsAndClear();

                foreach (var action in actions)
                {
                    await CardSkillExecutor.ExecuteCardActionAsync(action);
                    //TODO:需要等待动画播完ing
                }
                
                GameApp.MessageCenter.PostEvent(EventDefines.OnEnemyTurn);
            }
            catch (Exception e)
            {
                throw; // TODO 处理异常
            }
        }

        private async void onEnemyTurn(System.Object args)
        {
            try
            {
                Debug.Log("==== 敌人回合开始 ====");
                //BUG:敌人无法输出,待修复ing

                foreach (var enemy in GameApp.EntityManager.GetAliveEnemies())
                {
                    if(GameApp.EntityManager.GetAliveHeroes().Count == 0) break;

                    var cards = enemy.CharacterData.GetAllCards();
                    if(cards == null || cards.Count == 0)
                    {
                        Debug.LogWarning($"[{enemy.CharacterData.Name}] 没有可用的卡牌！");
                        continue;
                    }

                    CardData randomCard = cards[Random.Range(0, cards.Count)];

                    BattleCardData battleCard = new BattleCardData(randomCard);
                    CardAction enemyAction = new CardAction()
                    {
                        BattleCardData = battleCard,
                        OriginalIndex = -1,
                        TargetInstanceId = ""
                    };
                    
                    await CardSkillExecutor.ExecuteCardActionAsync(enemyAction);
                }

                if (GameApp.EntityManager.GetAliveHeroes().Count > 0 &&
                    GameApp.EntityManager.GetAliveEnemies().Count > 0)
                {
                    GameApp.MessageCenter.PostEvent(EventDefines.OnPlayerTurnStart);
                    StartNextRound();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"敌人回合发生异常: {e}"); 
            }
        }

        private void onSelectEnemyTarget(System.Object args)
        {
            string enemyInstanceId = args.ToString();
            GameApp.CardManager.CurrentSelectedTargetId = enemyInstanceId;
            
            //TODO:高亮显示被选中的敌人等

            Debug.Log($"选择了敌人目标，InstanceID：{enemyInstanceId}");
        }

        private void onCharacterDie(System.Object args)
        {
            BaseCharacter deadChar = args as BaseCharacter;
            
            if (deadChar == null) return;

            if (deadChar is HeroEntity hero)
                GameApp.EntityManager.GetAliveHeroes().Remove(hero);
            else if (deadChar is EnemyEntity enemy)
                GameApp.EntityManager.GetAliveEnemies().Remove(enemy);
            
            //判断胜负
            if (GameApp.EntityManager.GetAliveHeroes().Count == 0)
            {
                Debug.Log("==== 全部英雄死亡，玩家失败 ====");
                //TODO:打开失败界面等
            }
            else if (GameApp.EntityManager.GetAliveEnemies().Count == 0)
            {
                Debug.Log("==== 全部敌人死亡，玩家胜利 ====");
                //TODO:打开胜利界面等
            }
        }

        #endregion
        
        private void onSpawnBattleCallback()
        {
            GameApp.CardManager.InitCards(_currentInitData);

            GameApp.ViewManager.CloseAll();
            GameApp.ViewManager.Open(ViewType.FightingView);
        }
        
        private void StartFirstRound()
        {
            GameApp.CardManager.PrepareHandsForNewLevel();
            
            ApplyFunc(EventDefines.UpdateHandCards, GameApp.CardManager.GetHandCards());
        }

        private void StartNextRound()
        {
            int count = GameApp.CardManager.MaxHandCardCount - GameApp.CardManager.GetHandCards().Count;
            GameApp.CardManager.DrawCard(count);
            
            ApplyFunc(EventDefines.UpdateHandCards, GameApp.CardManager.GetHandCards());
        }
    }
}