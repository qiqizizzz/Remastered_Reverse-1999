/*
* ┌────────────────────────────────────────────┐
* │  描    述: 游戏主控制器(处理开始游戏 保存 退出等操作)                              
* │  类    名: GameController.cs       
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────┘
*/

using System;
using Common.Defines;
using MVC;
using MVC.Controller;
using UnityEngine;

namespace DefaultNamespace.Module.Game
{
    public class GameController : BaseController
    {
        public GameController() : base()
        {
            //暂时没有视图

            //GameApp.NetworkManager.Connect(); //连接服务器 - 暂时不使用网络功能，现在正在拼UI。。。
        }

        public override void Init()
        {
            //注册事件
            InitGlobalEvent();

            //ApplyControllerFunc(ControllerType.GameUI, EventDefines.OpenMainMenuView);
            ApplyControllerFunc(ControllerType.GameUI, EventDefines.OpenGameView);// 这个只是临时的
        }

        public override void InitGlobalEvent()
        {
            GameApp.MessageCenter.AddEvent(EventDefines.NetWork_Disconnect, onNetWorkDisconnect);
            GameApp.MessageCenter.AddEvent(EventDefines.NetWork_ConnectFailed, onNetWorkConnectFailed);
        }

        public override void RemoveGlobalEvent()
        {
            GameApp.MessageCenter.RemoveEvent(EventDefines.NetWork_Disconnect, onNetWorkDisconnect);
            GameApp.MessageCenter.RemoveEvent(EventDefines.NetWork_ConnectFailed, onNetWorkConnectFailed);
        }

        private void onNetWorkDisconnect(object args)
        {
            GameApp.ViewManager.Open(ViewType.NoticeView, "失去连接,确认重新连接?", new Action(() =>
                {
                    Debug.Log("正在重新连接...");
                    GameApp.NetworkManager.Connect();
                }),
                new Action(() =>
                {
                    Debug.Log("取消重新连接");
                    //TODO:返回主界面并且要清空当前游戏数据
                    //ApplyControllerFunc(ControllerType.GameUI, EventDefines.OpenMainMenuView);
                }));
        }

        private void onNetWorkConnectFailed(object args)
        {
            GameApp.ViewManager.Open(ViewType.NoticeView, "服务器暂未开放", new Action(() => { Debug.Log("已关闭界面..."); }));
        }
    }
}