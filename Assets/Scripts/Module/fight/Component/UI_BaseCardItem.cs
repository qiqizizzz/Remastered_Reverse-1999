/*
* ┌──────────────────────────────────┐
* │  描    述: 战斗卡牌实例(基类)                      
* │  类    名: UI_BaseCardItem.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using Data.card;
using DG.Tweening;
using MVC.View;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Module.fight.Component
{
    public class UI_BaseCardItem : BaseItem, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public BattleCardData BattleCardData;
        
        [Header("事件回调相关")] 
        private Action<UI_BaseCardItem, PointerEventData> OnBeginDragCallback;
        private Action<UI_BaseCardItem, PointerEventData> OnDragCallback;
        private Action<UI_BaseCardItem, PointerEventData> OnEndDragCallback;
        private Action<UI_BaseCardItem> OnClickCallback;

        [Header("动画配置")] 
        public CardAnimConfigSO AnimConfig;
        
        [Header("UI组件")] 
        public  RectTransform Rect;
        protected Image _icon;
        private CanvasGroup _canvasGroup;

        [Header("其他参数")] 
        protected readonly float MinScale = 0.75f;
        private bool _isDragging;
        public bool IsInQueue;
        
        public void SetBlockRaycasts(bool value) => _canvasGroup.blocksRaycasts = value;

        protected override void OnAwake()
        {
            base.OnAwake();
            
            Rect = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
        }
        
        public void InitCardUI(BattleCardData data)
        {
            BattleCardData = data;
            RefreshUI();
        }
        
        protected virtual void RefreshUI()
        {
            if(BattleCardData == null || BattleCardData.BaseData == null) return;

            _icon.sprite = BattleCardData.BaseData.CardSprite;
            
            _isDragging = false;
            IsInQueue = false;
        }
        
        #region 表现与动画逻辑
        public virtual void PrepareSpawn()
        {
            Rect.DOKill();
            transform.DOKill(); 
            
            SetVisible(false);
            _isDragging = false;
            IsInQueue = false;
            Rect.anchoredPosition = AnimConfig.SpawnPos;//放置在初始发牌点
            Rect.localScale = Vector3.one;
            Rect.localRotation = Quaternion.identity; 
        }
        
        public virtual void MoveToIndex(int index, int totalCount, float delay = 0f)
        {
            SetVisible(true);

            if (_isDragging)
            {
                transform.SetAsLastSibling();
                return;
            }

            float scale = 0.8f; 

            float currentWidth = AnimConfig.CardWidth * scale;
            float spacing = AnimConfig.CardSpacing * scale;

            float targetX = -AnimConfig.StartX - (totalCount - 1 - index) * (currentWidth + spacing);
            
            float targetY = GetMoveTargetY();
            
            Vector2 targetPos = new Vector2(targetX, targetY);

            Rect.DOKill();
            Rect.DOAnchorPos(targetPos, AnimConfig.MoveDuration)
                .SetEase(Ease.OutCubic)
                .SetDelay(delay);

            Rect.DOScale(Vector3.one * scale, AnimConfig.MoveDuration)
                .SetEase(Ease.OutCubic)
                .SetDelay(delay);

            Rect.transform.eulerAngles = Vector3.zero;

            transform.SetSiblingIndex(index);
        }
        
        protected virtual float GetMoveTargetY()
        {
            float scale = MinScale;
            return (scale - 1f) * (Rect.rect.height / 2f);
        }

        public virtual void PlayToQueueAnim(Vector2 targetPos, Action onComplete = null)
        {
            Rect.DOKill();
            Rect.DOScale(Vector3.one, AnimConfig.PlayCommonDuration);
            Rect.DOAnchorPos(targetPos, AnimConfig.PlayMoveDuration).SetEase(Ease.OutQuad).OnComplete(() => onComplete?.Invoke());
            Rect.DOScale(Vector3.one * AnimConfig.PlayQueueScale, AnimConfig.PlayCommonDuration).SetEase(Ease.OutQuad);
            Rect.DOBlendableLocalRotateBy(AnimConfig.PlayRotation, AnimConfig.PlayCommonDuration)
                .SetLoops(AnimConfig.PlayRotationLoop, LoopType.Yoyo);
        }

        public void PlayCompositeAnim(Vector2 targetPos, Action onComplete = null)
        {
            Rect.DOKill();
            transform.SetAsLastSibling();

            Sequence seq = DOTween.Sequence();
            seq.Append(Rect.DOAnchorPos(targetPos,AnimConfig.CompositeCommonDuration).SetEase(Ease.OutQuad));
            seq.Append(Rect.DOShakeAnchorPos(AnimConfig.CompositeCommonDuration, AnimConfig.CompositeStrength,
                AnimConfig.CompositeVibrato,
                AnimConfig.CompositeRandomMess));//抖动
            seq.OnComplete(() => onComplete?.Invoke());
        }
        
        public void HideCard()
        {
            Rect.DOKill();
            Rect.localScale = Vector3.one;
            Rect.localRotation = Quaternion.identity;
            IsInQueue = false;
            SetBlockRaycasts(true);
            SetVisible(false);
        }
        
        public void PlayFadeOutAnim(Action onComplete = null)
        {
            Rect.DOKill();
            _canvasGroup.DOKill();
            
            SetBlockRaycasts(false);
            _canvasGroup.DOFade(0f, 0.3f).OnComplete(() =>
            {
                HideCard();
                _canvasGroup.alpha = 1f; // 重置透明度以供对象池复用
                onComplete?.Invoke();
            });
        }
        
        #endregion
        
        #region 拖拽、点击事件相关
        public void OnBeginDrag(PointerEventData eventData)
        {
            if(IsInQueue) return;
            
            _isDragging = true;
            transform.SetAsLastSibling();//拖动时置于最上层
            
            Rect.DOKill();
            Rect.DOScale(Vector3.one * AnimConfig.DragScale, AnimConfig.DragDuration);
            _canvasGroup.blocksRaycasts = false;//拖动时允许穿透事件

            OnBeginDragCallback?.Invoke(this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if(IsInQueue) return;
            
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)transform.parent,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPos);

            Rect.anchoredPosition = new Vector2(localPos.x, 0);

            OnDragCallback?.Invoke(this, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if(IsInQueue) return;
            
            Rect.DOKill();
            Rect.DOScale(Vector3.one, AnimConfig.DragDuration);
            _canvasGroup.blocksRaycasts = true;

            _isDragging = false;
            OnEndDragCallback?.Invoke(this, eventData);
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if(_isDragging || IsInQueue) return;
            
            OnClickCallback?.Invoke(this);
        }
        #endregion
        
        #region 工具函数
        public int GetOwnerId() => BattleCardData.BaseData.OwnerId;
        
        public bool IsUltimateCard() => BattleCardData.BaseData.CardType == CardType.Ultimate;

        public void RegisterDragAndClickEvent(Action<UI_BaseCardItem, PointerEventData> onBeginDrag,
            Action<UI_BaseCardItem, PointerEventData> onDrag,
            Action<UI_BaseCardItem, PointerEventData> onEndDrag,
            Action<UI_BaseCardItem> onClick)
        {
            OnBeginDragCallback = onBeginDrag;
            OnDragCallback = onDrag;
            OnEndDragCallback = onEndDrag;
            OnClickCallback = onClick;
        }
        #endregion
    }
}