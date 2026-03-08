/*
* ┌─────────────────────────────────────┐
* │  描    述: 游戏面板主界面(打开各种UI的入口)                      
* │  类    名: GameView.cs       
* │  创    建: By qiqizizzz
* └─────────────────────────────────────┘
*/

using Common;
using MVC.View;
using UnityEngine;
using UnityEngine.UI;

namespace Module.View
{
    public class GameView : BaseView
    {
        protected override void OnAwake()
        {
            base.OnAwake();
            //注册
            Find<Button>("btns/txt").onClick.AddListener(onOpenButton1);
        }

        //测试打开主面板界面
        private void onOpenButton1()
        {
            Debug.Log("打开主界面");
        }
    }
}