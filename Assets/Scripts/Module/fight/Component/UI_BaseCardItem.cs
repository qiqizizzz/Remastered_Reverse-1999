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
        public Action<UI_BaseCardItem, PointerEventData> OnBeginDragCallback;
        public Action<UI_BaseCardItem, PointerEventData> OnDragCallback;
        public Action<UI_BaseCardItem, PointerEventData> OnEndDragCallback;
        public Action<UI_BaseCardItem> OnClickCallback;

        [Header("动画参数相关")]
        #region 卡牌移动相关
        public readonly float CardWidth = 180f;
        protected readonly float _cardSpacing = 10f;
        protected readonly float _startX = 90f;
        protected readonly float _moveDuration = 0.4f;
        protected Vector2 _spawnPos = new Vector2(-2200f, 0);
        #endregion
        #region 卡牌拖拽相关
        protected readonly float _dragScale = 1.15f;
        protected readonly float _dragDuration = 0.4f;
        #endregion
        #region 卡牌出牌相关
        protected readonly float _playCommonDuration = 0.2f;
        protected readonly float _playMoveDuration = 0.5f;
        protected readonly Vector3 _playRotation = new Vector3(0, 0, -10f);
        protected readonly float _playQueueScale = 0.8f;
        protected readonly int _playRotationLoop = 2;
        #endregion
        #region 卡牌合成相关
        protected readonly float _compositeCommonDuration = 0.2f;
        protected readonly float _compositeStrength = 25f;
        protected readonly int _compositeVibrato = 25;
        protected readonly float _compositeRandomMess = 45f;
        #endregion
        
        [Header("UI组件")] 
        public  RectTransform Rect;
        protected Image _icon;
        protected CanvasGroup _canvasGroup;
        
        [Header("其他参数")]
        protected bool _isDragging = false;
        public bool IsInQueue = false;
        
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
            
            Rect.localScale = Vector3.one;
            Rect.localRotation = Quaternion.identity;
            
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
            Rect.anchoredPosition = _spawnPos;//放置在初始发牌点
            Rect.localScale = Vector3.one;
            Rect.localRotation = Quaternion.identity; 
        }
        
        public void MoveToIndex(int index,int totalCount ,float delay = 0f)
        {
            SetVisible(true);
            
            if (_isDragging)
            {
                transform.SetAsLastSibling();
                return;
            }
            
            float targetX = -_startX - (totalCount - 1 - index) * (CardWidth + _cardSpacing);
            Vector2 targetPos = new Vector2(targetX, 0);

            Rect.DOKill();
            Rect.DOAnchorPos(targetPos, _moveDuration)
                .SetEase(Ease.OutCubic)
                .SetDelay(delay);
            
            Rect.DOScale(Vector3.one, _moveDuration)
                .SetEase(Ease.OutCubic)
                .SetDelay(delay);

            Rect.transform.eulerAngles = Vector3.zero; 
            
            transform.SetSiblingIndex(index);
        }

        public void PlayToQueueAnim(Vector2 targetPos, Action onComplete = null)
        {
            Rect.DOKill();
            Rect.DOScale(Vector3.one, _playCommonDuration);
            Rect.DOAnchorPos(targetPos, _playMoveDuration).SetEase(Ease.OutQuad).OnComplete(() => onComplete?.Invoke());
            Rect.DOScale(Vector3.one * _playQueueScale, _playCommonDuration).SetEase(Ease.OutQuad);
            Rect.DOBlendableLocalRotateBy(_playRotation, _playCommonDuration)
                .SetLoops(_playRotationLoop, LoopType.Yoyo);
        }

        public void PlayCompositeAnim(Vector2 targetPos, Action onComplete = null)
        {
            Rect.DOKill();
            transform.SetAsLastSibling();

            Sequence seq = DOTween.Sequence();
            seq.Append(Rect.DOAnchorPos(targetPos,_compositeCommonDuration).SetEase(Ease.OutQuad));
            seq.Append(Rect.DOShakeAnchorPos(_compositeCommonDuration, _compositeStrength, _compositeVibrato,
                _compositeRandomMess));//抖动
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
            Rect.DOScale(Vector3.one * _dragScale, _dragDuration);
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
            Rect.DOScale(Vector3.one, _dragDuration);
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
        public float GetMoveDuration() => _moveDuration;

        public int GetOwnerId() => BattleCardData.BaseData.OwnerId;
        
        public bool IsUltimateCard() => BattleCardData.BaseData.CardType == CardType.Ultimate;

        private static T GetCallback<T>(object[] args, int index) where T : class
        {
            return args.Length > index && args[index] is T callback ? callback : null;
        }
        
        public void RegisterDragAndClickEvent(params object[] args)
        {
            OnBeginDragCallback = GetCallback<Action<UI_BaseCardItem, PointerEventData>>(args, 1);
            OnDragCallback = GetCallback<Action<UI_BaseCardItem, PointerEventData>>(args, 2);
            OnEndDragCallback = GetCallback<Action<UI_BaseCardItem, PointerEventData>>(args, 3);
            OnClickCallback = GetCallback<Action<UI_BaseCardItem>>(args, 4);
        }
        #endregion
    }
}