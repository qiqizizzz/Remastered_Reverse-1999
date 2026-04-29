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

        // 通过重写 GetMoveTargetY 来修改 Y 轴偏移，避免同时启动两个 DOAnchorPos 动画导致竞争
        protected override float GetMoveTargetY()
        {
            float scale = 0.8f;
            return (scale - 1f) * (Rect.rect.height / 2f) + 30f;
        }

        // 大招卡牌预制体锚点可能与普通卡不一致，使用世界坐标动画绕过锚点差异
        public override void PlayToQueueAnim(Vector2 targetPos, Action onComplete = null)
        {
            Rect.DOKill();

            // 临时设置目标 anchoredPosition，让 Unity 自动算出正确的世界坐标，然后立即恢复
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