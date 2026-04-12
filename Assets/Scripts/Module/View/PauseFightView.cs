/*
* ┌──────────────────────────────────┐
* │  描    述: 暂停战斗界面                      
* │  类    名: PauseFightView.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using Common.Defines;
using Module.Loading;
using MVC;
using MVC.Extensions;
using MVC.View;
using UnityEngine;
using UnityEngine.UI;

namespace Module.View
{
    public class PauseFightView : BaseView
    {
        protected override void OnStart()
        {
            Find<Button>("Btn_continue").onClick.AddListener(onContinueBtn);
            Find<Button>("Btn_exit").onClick.AddListener(onExitBtn);
        }

        private void onExitBtn()
        {
            GameApp.ViewManager.Open(ViewType.NoticeView,"是否退出当前战斗?", new Action(() =>
            {
                ViewExtensions.LoadScene(this, SceneDefines.Game,() =>
                {
                    ApplyControllerFunc(ControllerType.GameUI, EventDefines.OpenGameView);
                });
            }),new Action(() =>
            {
                GameApp.ViewManager.Close(ViewId);
            }));
        }

        private void onContinueBtn()
        {
            GameApp.ViewManager.Close(ViewId);
        }
    }
}