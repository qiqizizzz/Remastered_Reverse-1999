/*
* ┌──────────────────────────────────┐
* │  描    述: 角色卡片                      
* │  类    名: CharacterCardItem.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using Data.card;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Module.Cultivation.ScrollComponents
{
    public class CharacterCardItem : MonoBehaviour
    {
        private int startIndex = 1001;
        private TextMeshProUGUI txt;
        private Tween _animTween;
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            txt = GetComponentInChildren<TextMeshProUGUI>();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        private void OnDisable()
        {
            _animTween?.Kill();
        }

        void ScrollCellIndex(int idx)
        {
            CharacterDataSO data = GameApp.ConfigManager.Character.Get(startIndex + idx);
            
            gameObject.name = "Card_" + data.Id;
            txt.text = data.Name;

            PlayEntryAnimation();
        }

        /// <summary>
        /// 播放入场翻转动画
        /// </summary>
        public void PlayEntryAnimation()
        {
            // 如果物体未激活（比如池中），不播放
            if (!gameObject.activeInHierarchy) return;

            _animTween?.Kill();

            // 初始状态：从左侧偏移，且绕Y轴旋转90度（侧面对着相机），透明度为0
            transform.localEulerAngles = new Vector3(0, 90f, 0);
            transform.localScale = Vector3.one * 0.8f;
            _canvasGroup.alpha = 0f;

            // 根据它在父节点下的层级来做延迟，形成错落有致的翻转感
            float delay = transform.GetSiblingIndex() * 0.05f;

            Sequence seq = DOTween.Sequence();
            seq.SetDelay(delay);
            seq.Append(transform.DOLocalRotate(Vector3.zero, 0.4f).SetEase(Ease.OutBack));
            seq.Join(transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack));
            seq.Join(_canvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutQuad));

            _animTween = seq;
        }
    }
}