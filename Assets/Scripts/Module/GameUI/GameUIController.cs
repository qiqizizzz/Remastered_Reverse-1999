/*
* ┌────────────────────────────────────────────────────────────┐
* │  描    述: 游戏主要面板控制器(任务、背包、关卡界面UI等在这里注册)                      
* │  类    名: GameUIController.cs       
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────────────┘
*/

using Common;
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
                PrefabName = "GameView",
                parentTf = GameApp.ViewManager.canvasTf,
                controller = this
            });
            
            //初始化事件
            InitModuleEvent();
            InitGlobalEvent();
            
            ApplyFunc(Defines.OpenGameView); // 这个只是临时的
        }

        // 注册事件
        public override void InitModuleEvent()
        {
            RegisterFunc(Defines.OpenGameView, OpenGameView);
        }

        //打开主要面板
        private void OpenGameView(System.Object[] args)
        {
            GameApp.ViewManager.Open(ViewType.GameView, args);
        }
    }
}