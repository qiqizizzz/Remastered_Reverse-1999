/*
* ┌──────────────────────────────────┐
* │  描    述: 更多选项界面（淡入动画）
* │  类    名: MoreOptionsView.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common.Defines;
using DG.Tweening;
using MVC;
using MVC.View;
using UnityEngine;
using UnityEngine.UI;

namespace Module.View
{
    public class MoreOptionsView : BaseView
    {
        private CanvasGroup _canvasGroup;
        private Tween _fadeTween;

        protected override void OnAwake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            Find<Button>("LeftUpArea/Btn_return").onClick.AddListener(onReturnGameViewBtn);
            Find<Button>("LeftDownArea/btns_1/Btn_hy").onClick.AddListener(onOpenChatViewBtn);
            Find<Button>("LeftDownArea/btns_2/Btn_sz").onClick.AddListener(onOpenSettingViewBtn);
        }

        public override void Open(params object[] args)
        {
            _canvasGroup.alpha = 0f;
            _fadeTween?.Kill();
            _fadeTween = _canvasGroup.DOFade(1f, 0.2f).SetEase(Ease.OutQuad);
        }
        
        private void onReturnGameViewBtn()
        {
            _fadeTween?.Kill();
            _fadeTween = _canvasGroup.DOFade(0f, 0.15f).SetEase(Ease.InQuad)
                .OnComplete(() => GameApp.ViewManager.NavigateBack());
        }

        private void onOpenChatViewBtn()
        {
            ApplyControllerFunc(ControllerType.Chat, EventDefines.OpenChatView);
        }
        
        private void onOpenSettingViewBtn()
        {
            GameApp.ViewManager.Open(ViewType.SettingView);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _fadeTween?.Kill();
            _canvasGroup.alpha = 1f;
        }

        protected override void OnDestroy()
        {
            _fadeTween?.Kill();
            base.OnDestroy();
        }
    }
}