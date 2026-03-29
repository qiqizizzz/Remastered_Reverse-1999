/*
* ┌─────────────────────────────────────┐
* │  描    述: 游戏面板主界面(打开各种UI的入口)                      
* │  类    名: GameView.cs       
* │  创    建: By qiqizizzz
* └─────────────────────────────────────┘
*/

using Common;
using Common.Defines;
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
            Find<Button>("RightArea/Btn_level").onClick.AddListener(onOpenLevelBtn);
            Find<Button>("LeftArea/btns/Btn_more").onClick.AddListener(onOpenMoreOptionsBtn);
            Find<Button>("RightArea/Btn_character").onClick.AddListener(onOpenCharacterBtn);
        }

        //测试打开level界面
        private void onOpenLevelBtn()
        {
            ApplyControllerFunc(ControllerType.Level, EventDefines.OpenLevelView);
            GameApp.ViewManager.Close(ViewId);
            //ViewExtensions.LoadScene(this, SceneDefines.LevelView);
        }
        
        //打开更多选项界面
        private void onOpenMoreOptionsBtn()
        {
            ApplyFunc(EventDefines.OpenMoreOptionsView);
        }

        private void onOpenCharacterBtn()
        {
            ApplyControllerFunc(ControllerType.Character, EventDefines.OpenCharacterView);
            GameApp.ViewManager.Close(ViewId);
        }
    }
}