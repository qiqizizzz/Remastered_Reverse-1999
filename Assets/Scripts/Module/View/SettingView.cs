/*
* ┌──────────────────────────────────┐
* │  描    述: 设置界面
* │  类    名: SettingView.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using Common;
using Common.Component;
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
        [Header("Tab 切换")]
        [SerializeField] private TabGroup TabGroup;

        [Header("图形设置")]
        private TMP_Dropdown resolutionDropdown;

        private Button _currentBtn;

        protected override void OnAwake()
        {
            TabGroup = GetComponent<TabGroup>();
            initTabGroup();

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

            resolutionDropdown.onValueChanged.AddListener(onResolutionDropdown);

            TabGroup.Init();
            TabGroup.OnTabChanged += onTabChanged;
            bindSelectAnim(TabGroup.CurrentButton);
        }

        private void initTabGroup()
        {
            var acBtn      = Find<Button>("btns/Btn_account");
            var graphicBtn = Find<Button>("btns/Btn_graphic");
            var soundBtn   = Find<Button>("btns/Btn_sound");
            var pushBtn    = Find<Button>("btns/Btn_push");
            var languageBtn = Find<Button>("btns/Btn_language");
            var gameBtn    = Find<Button>("btns/Btn_game");

            var acPanel      = Find<RectTransform>("panels/Panel_account").gameObject;
            var graphicPanel = Find<RectTransform>("panels/Panel_graphic").gameObject;
            var soundPanel   = Find<RectTransform>("panels/Panel_sound").gameObject;
            var pushPanel    = Find<RectTransform>("panels/Panel_push").gameObject;
            var languagePanel = Find<RectTransform>("panels/Panel_language").gameObject;
            var gamePanel    = Find<RectTransform>("panels/Panel_game").gameObject;

            TabGroup.SetTabs(new TabEntry[]
            {
                new TabEntry { TabButton = acBtn,      Panel = acPanel },
                new TabEntry { TabButton = graphicBtn, Panel = graphicPanel },
                new TabEntry { TabButton = soundBtn,   Panel = soundPanel },
                new TabEntry { TabButton = pushBtn,    Panel = pushPanel },
                new TabEntry { TabButton = languageBtn, Panel = languagePanel },
                new TabEntry { TabButton = gameBtn,    Panel = gamePanel },
            });
        }

        #region Panel选中动画
        private void onTabChanged(int index)
        {
            bindSelectAnim(TabGroup.CurrentButton);
        }

        private void bindSelectAnim(Button button)
        {
            if (_currentBtn != null)
            {
                if (_currentBtn == button) return;

                Transform oldArrow = _currentBtn.transform.Find("Img_selected");
                if (oldArrow != null)
                {
                    oldArrow.GetComponent<RectTransform>().DOKill();
                    oldArrow.gameObject.SetActive(false);
                }
            }

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

            _currentBtn = button;
        }
        #endregion

        #region 点击事件
        private void onReturnBtn()
        {
            GameApp.ViewManager.NavigateBack();
        }

        private void onExitBtn()
        {
            GameApp.ViewManager.Open(ViewType.NoticeView, "是否返回登录界面?", new Action(() =>
            {
                GameApp.ViewManager.CloseAll();
                GameApp.ViewManager.Open(ViewType.LoadingView);
                ViewExtensions.LoadScene(this, SceneDefines.StartMenu, () =>
                {
                    ApplyControllerFunc(ControllerType.GameUI, EventDefines.OpenMainMenuView);
                });
            }));
        }
        #endregion

        #region 分辨率设置
        private void onResolutionDropdown(int index)
        {
#if UNITY_EDITOR
            Debug.Log($"分辨率改变为{resolutionDropdown.options[index].text}");
#endif
            string text = resolutionDropdown.options[index].text;

            if (text == "窗口全屏" || text == "无边框全屏")
            {
                Resolution currentRes = Screen.currentResolution;
                if (text == "窗口全屏")
                    Screen.SetResolution(currentRes.width, currentRes.height, FullScreenMode.Windowed);
                else
                    Screen.SetResolution(currentRes.width, currentRes.height, FullScreenMode.FullScreenWindow);
            }
            else
            {
                string[] resArr = text.Split('*');
                if (resArr.Length == 2)
                {
                    if (int.TryParse(resArr[0], out int width) && int.TryParse(resArr[1], out int height))
                        Screen.SetResolution(width, height, FullScreenMode.Windowed);
                }
            }
        }
        #endregion

        #region 音量设置
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
    }
}
