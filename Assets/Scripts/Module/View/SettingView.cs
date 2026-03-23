/*
* ┌──────────────────────────────────┐
* │  描    述: 设置界面                      
* │  类    名: SettingView.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using Common;
using Common.Defines;
using DG.Tweening;
using Module.Loading;
using MVC;
using MVC.Extensions;
using MVC.View;
using UnityEngine;
using UnityEngine.UI;

namespace Module.View
{
    public class SettingView : BaseView
    {
        [Header("按钮UI")] 
        private Button acBtn;
        private Button graphicBtn;
        private Button soundBtn;
        private Button pushBtn;
        private Button languageBtn;
        private Button gameBtn;

        [Header("Panel UI")] 
        private RectTransform acPanel;
        private RectTransform graphicPanel;
        private RectTransform soundPanel;
        private RectTransform pushPanel;
        private RectTransform languagePanel;
        private RectTransform gamePanel;
        
        private Button currentBtn;
        private RectTransform currentPanel;

        protected override void OnAwake()
        {
            acBtn = Find<Button>("btns/Btn_account");
            graphicBtn = Find<Button>("btns/Btn_graphic");
            soundBtn = Find<Button>("btns/Btn_sound");
            pushBtn = Find<Button>("btns/Btn_push");
            languageBtn = Find<Button>("btns/Btn_language");
            gameBtn = Find<Button>("btns/Btn_game");
            
            acPanel = Find<RectTransform>("panels/Panel_account");
            graphicPanel = Find<RectTransform>("panels/Panel_graphic");
            soundPanel = Find<RectTransform>("panels/Panel_sound");
            pushPanel = Find<RectTransform>("panels/Panel_push");
            languagePanel = Find<RectTransform>("panels/Panel_language");
            gamePanel = Find<RectTransform>("panels/Panel_game");
        }

        protected override void OnStart()
        {
            Find<Button>("Btn_return").onClick.AddListener(onReturnBtn);
            Find<Button>("panels/Panel_account/others/Btn_exit").onClick.AddListener(onExitBtn);
            
            acBtn.onClick.AddListener(onAccountBtn);
            graphicBtn.onClick.AddListener(onGraphicBtn);
            soundBtn.onClick.AddListener(onSoundBtn);
            pushBtn.onClick.AddListener(onPushBtn);
            languageBtn.onClick.AddListener(onLanguageBtn);
            gameBtn.onClick.AddListener(onGameBtn);
            
            currentPanel = acPanel;
            bindSelectAnim(acBtn);
        }

        private void bindSelectAnim(Button button)
        {
            if (currentBtn != null)
            {
                if(currentBtn == button) return;
                Transform oldArrow = currentBtn.transform.Find("Img_selected");
                if (oldArrow != null)
                {
                    oldArrow.GetComponent<RectTransform>().DOKill();//清理之前的动画
                    oldArrow.gameObject.SetActive(false);//上一个按钮的此动画默认false
                }
            }
            
            //处理当前按钮的动画
            Transform newArrow = button.transform.Find("Img_selected");

            if (newArrow != null)
            {
                newArrow.gameObject.SetActive(true);
                RectTransform imgRect = newArrow.GetComponent<RectTransform>();
                imgRect.DOKill();
                Vector2 pos = imgRect.anchoredPosition;
                pos.x = -97;
                imgRect.anchoredPosition = pos;
                imgRect.DOAnchorPosX(-87, 0.5f).SetLoops(-1, LoopType.Yoyo);
            }
            
            currentBtn = button;
        }
        
        private void onReturnBtn()
        {
            GameApp.ViewManager.Close(ViewId);
        }

        #region 账号界面
        private void onAccountBtn()
        {
            bindSelectAnim(acBtn);
            
            currentPanel.gameObject.SetActive(false);
            acPanel.gameObject.SetActive(true);
            currentPanel = acPanel;
        }

        //退出登陆
        private void onExitBtn()
        {
            GameApp.ViewManager.Open(ViewType.NoticeView, "是否返回登录界面?", new Action(() =>
            {
                GameApp.ViewManager.CloseAll();
                GameApp.ViewManager.Open(ViewType.LoadingView);
                ViewExtensions.LoadScene(this,SceneDefines.StartMenuView,(() =>
                {
                    ApplyControllerFunc(ControllerType.GameUI, EventDefines.OpenMainMenuView);
                }));
            }));
        }
        #endregion

        #region 画面调节界面
        private void onGraphicBtn()
        {
            bindSelectAnim(graphicBtn);
            
            currentPanel.gameObject.SetActive(false);
            graphicPanel.gameObject.SetActive(true);
            currentPanel = graphicPanel;
        }
        
        #endregion

        #region 声音界面
        private void onSoundBtn()
        {
            bindSelectAnim(soundBtn);
            
            currentPanel.gameObject.SetActive(false);
            soundPanel.gameObject.SetActive(true);
            currentPanel = soundPanel;
        }
        #endregion

        #region 消息设置界面
        private void onPushBtn()
        {
            bindSelectAnim(pushBtn);
            
            currentPanel.gameObject.SetActive(false);
            pushPanel.gameObject.SetActive(true);
            currentPanel = pushPanel;
        }
        
        #endregion

        #region 语言设置界面
        private void onLanguageBtn()
        {
            bindSelectAnim(languageBtn);
            
            currentPanel.gameObject.SetActive(false);
            languagePanel.gameObject.SetActive(true);
            currentPanel = languagePanel;
        }
        #endregion

        #region 游戏设置界面
        private void onGameBtn()
        {
            bindSelectAnim(gameBtn);
            
            currentPanel.gameObject.SetActive(false);
            gamePanel.gameObject.SetActive(true);
            currentPanel = gamePanel;
        }
        #endregion
    }
}