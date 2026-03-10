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

        //测试打开主面板界面
        private void onOpenLevelBtn()
        {
            Debug.Log("打开Level界面");
            LoadingModel model = new LoadingModel();
            model.SetSceneName("level");
            model.callback = () => { Debug.Log("加载LevelScene完成回调"); };
            Controller.ApplyControllerFunc(ControllerType.Loading,Defines.LoadingScene, model);
        }
    }
}