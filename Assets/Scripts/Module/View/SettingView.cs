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
using Sound;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Module.View
{
    public class SettingView : BaseView
    {
        #region UI组件声明
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
        
        [Header("graphicPanel UI")]
        private TMP_Dropdown resolutionDropdown;
        #endregion
        
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
            
            resolutionDropdown = Find<TMP_Dropdown>("panels/Panel_graphic/view/Dropdown_resolution");
            
            Find<Slider>("panels/Panel_sound/sound/Slider_soundTotal").onValueChanged.AddListener(onSliderSoundTotalBtn);
            Find<Slider>("panels/Panel_sound/volume/Slider_soundBgm").onValueChanged.AddListener(onSliderSoundBgmBtn);
            Find<Slider>("panels/Panel_sound/volume/Slider_soundVoice").onValueChanged.AddListener(onSliderSoundVoiceBtn);
            Find<Slider>("panels/Panel_sound/volume/Slider_soundSfx").onValueChanged.AddListener(onSliderSoundSfxBtn);
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
            
            resolutionDropdown.onValueChanged.AddListener(onResolutionDropdown);
            
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
            GameApp.ViewManager.NavigateBack();
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
                ViewExtensions.LoadScene(this,SceneDefines.StartMenu,(() =>
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
        
        //分辨率设置
        private void onResolutionDropdown(int index)
        {
            Debug.Log($"分辨率改变为{resolutionDropdown.options[index].text}");
            string text = resolutionDropdown.options[index].text;

            if (text == "窗口全屏" || text == "无边框全屏")
            {
                Resolution currentRes = Screen.currentResolution;//获取当前屏幕分辨率
                if(text == "窗口全屏")
                {
                    Screen.SetResolution(currentRes.width,currentRes.height,FullScreenMode.Windowed);
                }
                else
                {
                    Screen.SetResolution(currentRes.width,currentRes.height,FullScreenMode.FullScreenWindow);
                }
            }
            else
            {
                string[] resArr = text.Split('*');

                if (resArr.Length == 2)
                {
                    if (int.TryParse(resArr[0], out int width) && int.TryParse(resArr[1], out int height))
                    {
                        Screen.SetResolution(width, height, FullScreenMode.Windowed);
                    }
                }
            }

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

        private void onSliderSoundTotalBtn(float volume)
        {
            GameApp.SoundManager.TotalVolume = volume;
        }
        
        private void onSliderSoundBgmBtn(float volume)
        {
            GameApp.SoundManager.BgmVolume = volume;
        }
        
        private void onSliderSoundVoiceBtn(float volume)
        {
            GameApp.SoundManager.VoiceVolume = volume;
        }
        
        private void onSliderSoundSfxBtn(float volume)
        {
            GameApp.SoundManager.EffectVolume = volume;
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