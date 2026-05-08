/*
* ┌──────────────────────────────────────────────────────────────────────────────────────────────────────┐
* │  描    述: 战斗控制器(只负责管理宏观的战斗流程(控制回合)，包括回合开始、回合结束、战斗结束等，不涉及具体的战斗逻辑)
* │  类    名: FightController.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────────────────────────────────────────────────────────────────────────┘
*/

using System;
using Common.Defines;
using Data.level;
using GameProtocol;
using Module.Character;
using Module.fight.CardMgr;
using Module.fight.Core.Entities;
using Module.fight.Network;
using MVC;
using MVC.Controller;
using UnityEngine;

namespace Module.fight
{
    public class FightController : BaseController
    {
        private LevelModel _currentModel;
        private bool _isBattleActive;//是否还在战斗中
        private bool _isPlayerTurnStart; //是否是玩家回合开始（不包括输出回合）
        private readonly BattleNetworkController _battleNetwork;
        
        public FightController() : base()
        {
            _battleNetwork = new BattleNetworkController();
            _battleNetwork.Init();
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
                Sorting_Order = 5
            });
            GameApp.ViewManager.Register(ViewType.FightSettleView, new ViewInfo()
            {
                PrefabName =  AddressDefines.UI_FightSettleView,
                controller = this,
                parentTf = GameApp.ViewManager.canvasTf,
                Sorting_Order = 10
            });
            
            
            InitModuleEvent();
        }

        public sealed override void InitModuleEvent()
        {
            RegisterFunc(EventDefines.OpenFightingView, onOpenFightView);
            RegisterFunc(EventDefines.OpenPauseFightView, onOpenPauseFightView);
            RegisterFunc(EventDefines.OpenFightSettleView, onOpenFightSettleView);
            
            GameApp.MessageCenter.AddEvent(EventDefines.FightingViewReady, onFightingViewReady);
            GameApp.MessageCenter.AddEvent(EventDefines.OnPlayerTurnStart, onPlayerTurnStart);
            GameApp.MessageCenter.AddEvent(EventDefines.OnSelectEnemyTarget, onSelectEnemyTarget);
            GameApp.MessageCenter.AddEvent(EventDefines.OnCharacterDie, onCharacterDie);
            GameApp.MessageCenter.AddEvent(EventDefines.OnBattleServerResponse, onBattleServerResponse);
        }

        public override void RemoveModuleEvent()
        {
            UnRegisterFunc(EventDefines.OpenFightingView, onOpenFightView);
            UnRegisterFunc(EventDefines.OpenPauseFightView, onOpenPauseFightView);
            UnRegisterFunc(EventDefines.OpenFightSettleView, onOpenFightSettleView);
            
            GameApp.MessageCenter.RemoveEvent(EventDefines.FightingViewReady, onFightingViewReady);
            GameApp.MessageCenter.RemoveEvent(EventDefines.OnPlayerTurnStart, onPlayerTurnStart);
            GameApp.MessageCenter.RemoveEvent(EventDefines.OnSelectEnemyTarget, onSelectEnemyTarget);
            GameApp.MessageCenter.RemoveEvent(EventDefines.OnCharacterDie, onCharacterDie);
            GameApp.MessageCenter.RemoveEvent(EventDefines.OnBattleServerResponse, onBattleServerResponse);
            
            _battleNetwork.UnInit();
        }

        #region UI事件
        private void onOpenFightView(System.Object[] args)
        {
            _currentModel = args[0] as LevelModel;
            
            GameApp.EntityManager.SpawnBattleEntities(_currentModel, onSpawnBattleCallback);//生成玩家与敌人等
        }
        
        private void onOpenFightSettleView(System.Object[] args)
        {
            bool isWin = (bool)args[0];
            
            GameApp.ViewManager.Open(ViewType.FightSettleView, isWin);
        }

        private void onOpenPauseFightView(System.Object[] args)
        {
            GameApp.ViewManager.Open(ViewType.PauseFightView);
        }
        
        private void onFightingViewReady(System.Object args)
        {
            // 战斗准备就绪，等待服务端事件驱动
        }

        // 接收并播放服务端推送的战斗事件序列
        private void onBattleServerResponse(object args)
        {
            var battlePack = args as BattlePack;
            if (battlePack == null) return;
            PlayEventSequence.Play(battlePack.Events);
        }
        #endregion

        #region 战斗事件
        private void onPlayerTurnStart(object args)
        {
            Debug.Log("==== 玩家回合开始 ====");
            _isPlayerTurnStart = true;
        }

        private void onSelectEnemyTarget(object args)
        {
            if(!_isPlayerTurnStart) return;
            
            int enemyInstanceId = (int)args;
            GameApp.CardManager.CurrentSelectedTargetId = enemyInstanceId;
            
            foreach (var enemy in GameApp.EntityManager.GetAliveEnemies())
            {
                enemy.HUD?.SetSelected(enemy.CombatInstanceId == enemyInstanceId);
            }
        }

        private void onCharacterDie(object args)
        {
            BaseCharacter deadChar = args as BaseCharacter;
            
            if (deadChar == null) return;

            if (deadChar is HeroEntity hero)
            {
                GameApp.EntityManager.GetAliveHeroes().Remove(hero);
                GameApp.CardManager.RemoveDiedCharacterCard(deadChar.CharacterData);
            }
            else if (deadChar is EnemyEntity enemy)
                GameApp.EntityManager.GetAliveEnemies().Remove(enemy);
            
            // 胜负判断由服务端 BattleEnd 事件驱动（PlayEventSequence 处理）
        }

        #endregion

        #region 回合相关事件
        private void onSpawnBattleCallback()
        {
            _isBattleActive = true;

            GameApp.CardManager.InitCards(_currentModel);
            
            foreach (var hero in GameApp.EntityManager.GetAliveHeroes())
            {
                GameApp.CardManager.BattleContext.Entities[hero.CharacterData.Id] = new CombatEntity(
                    hero.CombatInstanceId,
                    hero.CharacterData.Id,
                    1,
                    hero.CurrentHp,
                    hero.ActionPoint
                );
            }
            
            GameApp.ViewManager.CloseAll();
            GameApp.ViewManager.Open(ViewType.FightingView);
            
            // 由服务端驱动战斗初始化与回合流转
            _battleNetwork.SendEnterPve(_currentModel.LevelId);
        }
        #endregion
    }
}