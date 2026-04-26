/*
* ┌──────────────────────────────────┐
* │  描    述: 战斗卡牌实例(普通卡牌)
* │  类    名: UI_CardItem.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Data.card;
using DG.Tweening;
using MVC.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Module.fight.Component
{
    public class UI_CommonCardItem : BaseItem, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public BattleCardData BattleCardData { get; private set; }

        [Header("事件回调相关")] 
        public Action<UI_CommonCardItem, PointerEventData> OnBeginDragCallback;
        public Action<UI_CommonCardItem, PointerEventData> OnDragCallback;
        public Action<UI_CommonCardItem, PointerEventData> OnEndDragCallback;
        public Action<UI_CommonCardItem> OnClickCallback;
        
        [Header("动画参数相关")]
        #region 卡牌移动相关
        public readonly float CardWidth = 180f;
        private readonly float _cardSpacing = 10f;
        private readonly float _startX = 90f;
        private readonly float _moveDuration = 0.4f;
        private Vector2 _spawnPos = new Vector2(-2200f, 0);
        #endregion
        #region 卡牌拖拽相关
        private readonly float _dragScale = 1.15f;
        private readonly float _dragDuration = 0.4f;
        #endregion
        #region 卡牌出牌相关
        private readonly float _playCommonDuration = 0.2f;
        private readonly float _playMoveDuration = 0.5f;
        private readonly Vector3 _playRotation = new Vector3(0, 0, -10f);
        private readonly float _playQueueScale = 0.8f;
        private readonly int _playRotationLoop = 2;
        #endregion
        #region 卡牌合成相关
        private readonly float _compositeCommonDuration = 0.2f;
        private readonly float _compositeStrength = 25f;
        private readonly int _compositeVibrato = 25;
        private readonly float _compositeRandomMess = 45f;
        #endregion
        
        [Header("UI组件")] 
        public  RectTransform Rect;
        private Image _icon;
        private CanvasGroup _canvasGroup;
        private List<CanvasGroup> _typeGroups = new List<CanvasGroup>();
        private List<Image> _stars = new List<Image>();
        
        [Header("其他参数")]
        private readonly int _maxStarLevel = 3;
        
        private bool _isDragging = false;
        public bool IsInQueue = false;
        
        public void SetBlockRaycasts(bool value) => _canvasGroup.blocksRaycasts = value;
        
        protected override void OnAwake()
        {
            Rect = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            _icon = Find<Image>("Img_card");
            
            _typeGroups.Add(Find<CanvasGroup>("type/Attack"));
            _typeGroups.Add(Find<CanvasGroup>("type/DeBuff"));
            _typeGroups.Add(Find<CanvasGroup>("type/Buff"));
            _typeGroups.Add(Find<CanvasGroup>("type/Health"));
            _typeGroups.Add(Find<CanvasGroup>("type/Channel"));
            
            _stars.Add(Find<Image>("Star/star_1/star_open"));
            _stars.Add(Find<Image>("Star/star_2/star_open"));
            _stars.Add(Find<Image>("Star/star_3/star_open"));
        }
        
        public void InitCardUI(BattleCardData data)
        {
            BattleCardData = data;
            RefreshUI();
        }

        public void ShowStarUI(int starCount)
        {
            starCount = Mathf.Clamp(starCount, 0, _maxStarLevel);
            
            for (int i = 0; i < _maxStarLevel; i++)
            {
                _stars[i].gameObject.SetActive(i < starCount);
            }
        }
        
        private void RefreshUI()
        {
            if(BattleCardData == null || BattleCardData.BaseData == null) return;

            _icon.sprite = BattleCardData.BaseData.CardSprite;
            showTypeUI((int)BattleCardData.BaseData.CardType);
            ShowStarUI(BattleCardData.StarLevel);
            
            Rect.localScale = Vector3.one;
            Rect.localRotation = Quaternion.identity;
            _isDragging = false;
            IsInQueue = false;
            
            SetBlockRaycasts(true);
        }
        
        private void showTypeUI(int index)
        {
            for (int i = 0; i < _typeGroups.Count; i++)
            {
                _typeGroups[i].alpha = (i == index) ? 1 : 0;
                _typeGroups[i].blocksRaycasts = (i == index);
                _typeGroups[i].interactable = (i == index);
            }
        }
        
        #region 表现与动画逻辑
        public void PrepareSpawn()
        {
            Rect.DOKill();
            transform.DOKill(); 
            
            SetVisible(false);
            _isDragging = false;
            IsInQueue = false;
            Rect.anchoredPosition = _spawnPos;//放置在初始发牌点
            Rect.localScale = Vector3.one;
            Rect.localRotation = Quaternion.identity; 

            ShowStarUI(1);
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

        #endregion
    }
}