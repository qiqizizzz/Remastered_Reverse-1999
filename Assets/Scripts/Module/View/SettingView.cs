/*
* ┌──────────────────────────────────┐
* │  描    述: 设置界面                      
* │  类    名: SettingView.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using DG.Tweening;
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

        private Button preBtn;

        protected override void OnAwake()
        {
            acBtn = Find<Button>("btns/Btn_account");
            graphicBtn = Find<Button>("btns/Btn_graphic");
            soundBtn = Find<Button>("btns/Btn_sound");
            pushBtn = Find<Button>("btns/Btn_push");
            languageBtn = Find<Button>("btns/Btn_language");
            gameBtn = Find<Button>("btns/Btn_game");
        }

        protected override void OnStart()
        {
            Find<Button>("Btn_return").onClick.AddListener(onReturnBtn);
            
            acBtn.onClick.AddListener(onAccountBtn);
            graphicBtn.onClick.AddListener(onGraphicBtn);
            soundBtn.onClick.AddListener(onSoundBtn);
            pushBtn.onClick.AddListener(onPushBtn);
            languageBtn.onClick.AddListener(onLanguageBtn);
            gameBtn.onClick.AddListener(onGameBtn);
            
            bindSelectAnim(acBtn);
        }

        private void bindSelectAnim(Button button)
        {
            if(preBtn != null)
                preBtn.transform.Find("Img_select").gameObject.SetActive(false);//上一个按钮的此动画默认false
            
            button.transform.Find("Img_select").gameObject.SetActive(true);
            button.GetComponentInChildren<Image>().rectTransform.DOAnchorPosX(10f, 0.5f).SetRelative()
                .SetLoops(-1, LoopType.Yoyo);
            
            preBtn = button;
        }
        
        private void onReturnBtn()
        {
            GameApp.ViewManager.Close(ViewId);
        }

        #region 账号界面
        private void onAccountBtn()
        {
            bindSelectAnim(acBtn);
            Debug.Log("账号设置");
        }
        #endregion

        #region 画面调节界面
        private void onGraphicBtn()
        {
            bindSelectAnim(graphicBtn);
            Debug.Log("画面设置");
        }
        
        #endregion

        #region 声音界面
        private void onSoundBtn()
        {
            bindSelectAnim(soundBtn);
            Debug.Log("声音设置");
        }
        #endregion

        #region 消息设置界面
        private void onPushBtn()
        {
            bindSelectAnim(pushBtn);
            Debug.Log("推送设置");
        }
        
        #endregion

        #region 语言设置界面
        private void onLanguageBtn()
        {
            bindSelectAnim(languageBtn);
            Debug.Log("语言设置");
        }
        #endregion

        #region 游戏设置界面
        private void onGameBtn()
        {
            bindSelectAnim(gameBtn);
            Debug.Log("游戏设置");
        }
        #endregion
    }
}