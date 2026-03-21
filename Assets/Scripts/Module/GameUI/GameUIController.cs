/*
* ┌────────────────────────────────────────────────────────────┐
* │  描    述: 游戏主要面板控制器(主界面,主菜单,提示面板UI等在这里注册)                      
* │  类    名: GameUIController.cs       
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────────────┘
*/

using Common;
using Common.Defines;
using MVC;
using MVC.Controller;
using UnityEngine;

namespace Module.GameUI
{
    public class GameUIController : BaseController
    {
        public GameUIController() : base()
        {
            //注册视图
            GameApp.ViewManager.Register(ViewType.GameView, new ViewInfo()
            {
                PrefabName = AddressDefines.UI_GameView,
                parentTf = GameApp.ViewManager.canvasTf,
                controller = this
            });
            GameApp.ViewManager.Register(ViewType.MainMenuView,new ViewInfo()
            {
                PrefabName = AddressDefines.UI_MainMenuView,
                parentTf = GameApp.ViewManager.canvasTf,
                controller = this
            });
            GameApp.ViewManager.Register(ViewType.MoreOptionsView, new ViewInfo()
            {
                PrefabName = AddressDefines.UI_MoreOptionsView,
                parentTf = GameApp.ViewManager.canvasTf,
                controller = this,
                Sorting_Order = 1
            });
            GameApp.ViewManager.Register(ViewType.TipBoxView, new ViewInfo()
            {
                PrefabName = AddressDefines.UI_Small_TipBox,
                parentTf = GameApp.ViewManager.canvasTf,
                controller = this,
                Sorting_Order = 999
            });
            
            //初始化事件
            InitModuleEvent();
            InitGlobalEvent();
        }

        // 注册事件
        public override void InitModuleEvent()
        {
            RegisterFunc(EventDefines.OpenGameView, openGameView);
            RegisterFunc(EventDefines.OpenMainMenuView, openMainMenuView);
            RegisterFunc(EventDefines.OpenMoreOptionsView, onOpenMoreOptionsView);
            RegisterFunc(EventDefines.OpenTipBoxView, onOpenTipBoxView);
        }

        //打开主要面板
        private void openGameView(System.Object[] args)
        {
            GameApp.ViewManager.Open(ViewType.GameView, args);
        }
        
        //打开主菜单界面
        private void openMainMenuView(System.Object[] args)
        {
            GameApp.ViewManager.Open(ViewType.MainMenuView, args);
        }
        
        //打开更多选项界面
        private void onOpenMoreOptionsView(System.Object[] args)
        {
            GameApp.ViewManager.Open(ViewType.MoreOptionsView, args);
        }

        private void onOpenTipBoxView(System.Object[] args)
        {
            GameApp.ViewManager.Open(ViewType.TipBoxView, args);
        }
    }
}