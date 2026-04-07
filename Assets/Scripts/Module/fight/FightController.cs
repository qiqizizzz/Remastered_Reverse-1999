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
        public FightController() : base()
        {
            GameApp.ViewManager.Register(ViewType.FightingView, new ViewInfo()
            {
                PrefabName = AddressDefines.UI_FightingView,
                controller = this,
                parentTf = GameApp.ViewManager.canvasTf
            });
            
            InitModuleEvent();
        }

        public override void InitModuleEvent()
        {
            RegisterFunc(EventDefines.OpenFightingView, onOpenFightView);
        }
        
        private void onOpenFightView(System.Object[] args)
        {
            LevelInitData initData = args[0] as LevelInitData;
            
            GameApp.EntityManager.SpawnBattleEntities(initData);//生成玩家与敌人等
            
            GameApp.ViewManager.Open(ViewType.FightingView, args);
        }
    }
}