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
            GameApp.ViewManager.Register(ViewType.MainMenuView, new ViewInfo()
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
                PrefabName = AddressDefines.UI_TipBoxView,
                parentTf = GameApp.ViewManager.canvasTf,
                controller = this,
                Sorting_Order = 998
            });
            GameApp.ViewManager.Register(ViewType.NoticeView, new ViewInfo()
            {
                PrefabName = AddressDefines.UI_NoticeView,
                parentTf = GameApp.ViewManager.canvasTf,
                controller = this,
                Sorting_Order = 999
            });
            GameApp.ViewManager.Register(ViewType.SettingView, new ViewInfo()
            {
                PrefabName = AddressDefines.UI_SettingView,
                parentTf = GameApp.ViewManager.canvasTf,
                controller = this,
                Sorting_Order = 10
            });

            //初始化事件
            InitModuleEvent();
            InitGlobalEvent();
        }

        // 注册事件
        public override void InitModuleEvent()
        {
            RegisterFunc(EventDefines.OpenGameView, onOpenGameView);
            RegisterFunc(EventDefines.OpenMainMenuView, onOpenMainMenuView);
            RegisterFunc(EventDefines.OpenMoreOptionsView, onOpenMoreOptionsView);
            RegisterFunc(EventDefines.OpenTipBoxView, onOpenTipBoxView);
            RegisterFunc(EventDefines.OpenNoticeView, onOpenNoticeView);
            RegisterFunc(EventDefines.OpenSettingView, onOpenSettingView);
        }

        //打开主要面板
        private void onOpenGameView(System.Object[] args)
        {
            GameApp.ViewManager.CloseAll();
            
            GameApp.ViewManager.Open(ViewType.GameView, args);
        }

        //打开主菜单界面
        private void onOpenMainMenuView(System.Object[] args)
        {
            GameApp.ViewManager.CloseAll();
            
            GameApp.ViewManager.Open(ViewType.MainMenuView, args);
        }

        //打开更多选项界面
        private void onOpenMoreOptionsView(System.Object[] args)
        {
            GameApp.ViewManager.Open(ViewType.MoreOptionsView, args);
        }

        //打开提示框界面
        private void onOpenTipBoxView(System.Object[] args)
        {
            GameApp.ViewManager.Open(ViewType.TipBoxView, args);
        }

        //打开提示界面
        private void onOpenNoticeView(System.Object[] args)
        {
            GameApp.ViewManager.Open(ViewType.NoticeView, args);
        }
        
        //打开设置界面
        private void onOpenSettingView(System.Object[] args)
        {
            GameApp.ViewManager.Open(ViewType.SettingView, args);
        }
    }
}
