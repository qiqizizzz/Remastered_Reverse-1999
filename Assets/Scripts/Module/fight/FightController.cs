/*
* ┌──────────────────────────────────────────────────────────────────────────────────────────────────────┐
* │  描    述: 战斗控制器(只负责管理宏观的战斗流程(控制回合)，包括回合开始、回合结束、战斗结束等，不涉及具体的战斗逻辑)
* │  类    名: FightController.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────────────────────────────────────────────────────────────────────────┘
*/

using System.Collections.Generic;
using Common.Defines;
using Data.level;
using Module.fight.CardMgr;
using Module.fight.Skill;
using MVC;
using MVC.Controller;
using UnityEngine;

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
        
        private void onPlayerTurnOutput(System.Object args)
        {
            Debug.Log("==== 玩家出牌阶段结束，开始结算队列 ====");
            
            //TODO:禁用UI交互,播放出牌动画等...
            
            List<CardAction> actions = GameApp.CardManager.CardActionQueue.GetAllActionsAndClear();

            foreach (var action in actions)
            {
                CardSkillExecutor.ExecuteCardAction(action);
                
                //TODO:需要等待动画播完ing
            }
            
            GameApp.MessageCenter.PostEvent(EventDefines.OnEnemyTurn);
        }

        private void onEnemyTurn(System.Object args)
        {
            Debug.Log("==== 敌人回合开始 ====");
            
            //TODO:...
            
            GameApp.MessageCenter.PostEvent(EventDefines.OnPlayerTurnStart);
        }

        private void onSelectEnemyTarget(System.Object args)
        {
            string enemyInstanceId = args.ToString();
            GameApp.CardManager.CurrentSelectedTargetId = enemyInstanceId;

            Debug.Log($"选择了敌人目标，InstanceID：{enemyInstanceId}");
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
            //GameApp.CardManager.PrepareHandsForNewLevel();后续需要改回来ing
            GameApp.CardManager.DrawCard(8);
            
            ApplyFunc(EventDefines.UpdateHandCards, GameApp.CardManager.GetHandCards());
        }
    }
}