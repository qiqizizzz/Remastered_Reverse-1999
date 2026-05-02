/*
* ┌──────────────────────────────────┐
* │  描    述: 战斗卡牌实例(大招卡牌)                      
* │  类    名: UI_UltimateCardItem.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Module.fight.Component
{
    public class UI_UltimateCardItem : UI_BaseCardItem
    {
        protected override void OnAwake()
        {
            base.OnAwake();
            
            _icon = Find<Image>("mask/Img_card");
        }
        
        protected override float GetMoveTargetY()
        {
            float scale = MinScale;
            return (scale - 1f) * (Rect.rect.height / 2f) + 30f;
        }
        
        public override void PlayToQueueAnim(Vector2 targetPos, Action onComplete = null)
        {
            Rect.DOKill();
            
            Vector2 originalAnchorPos = Rect.anchoredPosition;
            Rect.anchoredPosition = targetPos;
            Vector3 targetWorldPos = Rect.position;
            Rect.anchoredPosition = originalAnchorPos;

            Rect.DOScale(Vector3.one, AnimConfig.PlayCommonDuration);
            Rect.DOMove(targetWorldPos, AnimConfig.PlayMoveDuration).SetEase(Ease.OutQuad).OnComplete(() => onComplete?.Invoke());
            Rect.DOScale(Vector3.one * AnimConfig.PlayQueueScale, AnimConfig.PlayCommonDuration).SetEase(Ease.OutQuad);
            Rect.DOBlendableLocalRotateBy(AnimConfig.PlayRotation, AnimConfig.PlayCommonDuration)
                .SetLoops(AnimConfig.PlayRotationLoop, LoopType.Yoyo);
        }
    
    }
}