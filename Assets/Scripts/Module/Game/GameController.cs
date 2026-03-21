/*
* ┌────────────────────────────────────────────┐
* │  描    述: 游戏主控制器(处理开始游戏 保存 退出等操作)                              
* │  类    名: GameController.cs       
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────┘
*/

using Common.Defines;
using MVC;
using MVC.Controller;

namespace DefaultNamespace.Module.Game
{
    public class GameController : BaseController
    {
        public GameController() : base()
        {
            //暂时没有视图
            
            GameApp.NetworkManager.Connect(); //连接服务器 - 暂时不使用网络功能，现在正在拼UI。。。
        }

        public override void Init()
        {
            ApplyControllerFunc(ControllerType.GameUI, EventDefines.OpenMainMenuView); 
            //ApplyControllerFunc(ControllerType.GameUI, EventDefines.OpenGameView);// 这个只是临时的
        }
    }
}