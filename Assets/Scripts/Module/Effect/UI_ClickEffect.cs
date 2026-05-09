/*
* ┌──────────────────────────────────┐
* │  描    述: 鼠标点击涟漪特效，池化复用
* │  类    名: UI_ClickEffect.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Module.Effect
{
    public class UI_ClickEffect : MonoBehaviour
    {
        [SerializeField] private Image RippleImage;
        [SerializeField] private float Duration = 0.35f;
        [SerializeField] private float MaxScale = 1.5f;
        [SerializeField] private string PoolKey = "Effect/ClickRipple";

        private CanvasGroup _canvasGroup;
        private Sequence _animSeq;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (RippleImage == null)
                RippleImage = GetComponent<Image>();

            _canvasGroup.alpha = 0f;
            transform.localScale = Vector3.zero;
        }

        private void OnDisable()
        {
            _animSeq?.Kill();
            transform.localScale = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            _canvasGroup.alpha = 0f;
        }

        public void Play(Vector3 screenPos, Transform parent = null)
        {
            if (parent != null)
                transform.SetParent(parent, false);

            transform.position = screenPos;
            transform.SetAsLastSibling();
            transform.localRotation = Quaternion.identity;

            _canvasGroup.alpha = 1f;
            transform.localScale = Vector3.zero;

            _animSeq?.Kill();
            _animSeq = DOTween.Sequence();
            _animSeq.Join(transform.DOScale(MaxScale, Duration).SetEase(Ease.OutBack, 1.2f));
            _animSeq.Join(_canvasGroup.DOFade(0f, Duration).SetEase(Ease.InSine));
            _animSeq.OnComplete(recycle);
        }

        private void recycle()
        {
            ResManager.ReleaseToPool(PoolKey, gameObject);
        }
    }
}
