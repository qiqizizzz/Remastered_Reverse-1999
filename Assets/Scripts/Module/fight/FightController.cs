/*
* ┌──────────────────────────────────────────────────────────────────────────────────────────────────────┐
* │  描    述: 战斗控制器(只负责管理宏观的战斗流程(控制回合)，包括回合开始、回合结束、战斗结束等，不涉及具体的战斗逻辑)
* │  类    名: FightController.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────────────────────────────────────────────────────────────────────────┘
*/

using Common.Defines;
using Data.level;
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
        }
        
        private void onOpenFightView(System.Object[] args)
        {
            _currentInitData = args[0] as LevelInitData;
            
            GameApp.EntityManager.SpawnBattleEntities(_currentInitData, onSpawnBattleCallback);//生成玩家与敌人等
        }

        private void onOpenPauseFightView(System.Object[] args)
        {
            GameApp.ViewManager.Open(ViewType.PauseFightView);
        }
        
        private void onSpawnBattleCallback()
        {
            GameApp.CardManager.InitCards(_currentInitData);

            GameApp.ViewManager.CloseAll();
            GameApp.ViewManager.Open(ViewType.FightingView);
            
            StartFirstRound();
        }

        private void StartFirstRound()
        {
            GameApp.CardManager.DrawCard(8);
            
            ApplyFunc(EventDefines.UpdateHandCards, GameApp.CardManager.GetHandCards());
        }
    }
}