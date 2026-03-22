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

        protected override void OnAwake()
        {
            acBtn = Find<Button>("btns/Btn_account");
        }

        protected override void OnStart()
        {
            Find<Button>("Btn_return").onClick.AddListener(onReturnBtn);
            
            acBtn.onClick.AddListener(onAccountBtn);
            
            bindSelectAnim();
        }

        private void bindSelectAnim()
        {
            acBtn.GetComponentInChildren<Image>().rectTransform.DOAnchorPosX(10f, 0.5f).SetRelative()
                .SetLoops(-1, LoopType.Yoyo);
            
            //其他按钮的此动画默认false
        }
        
        private void onReturnBtn()
        {
            GameApp.ViewManager.Close(ViewId);
        }

        private void onAccountBtn()
        {
            
        }
    }
}