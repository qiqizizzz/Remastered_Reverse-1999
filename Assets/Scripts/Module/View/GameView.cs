/*
* ┌─────────────────────────────────────┐
* │  描    述: 游戏面板主界面(打开各种UI的入口)                      
* │  类    名: GameView.cs       
* │  创    建: By qiqizizzz
* └─────────────────────────────────────┘
*/

using Common;
using Module.Loading;
using MVC;
using MVC.Extensions;
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
            Find<Button>("RightArea/level").onClick.AddListener(onOpenLevelBtn);
        }

        //测试打开level界面
        private void onOpenLevelBtn()
        {
            Debug.Log("打开Level界面");
            Close();
            ViewExtensions.LoadScene(this, SceneDefines.LevelView);
        }
    }
}