/*
* ┌──────────────────────────────────┐
* │  描    述: 行动队列UI管理器                      
* │  类    名: ActionQueueUIManager.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using DG.Tweening;
using Module.fight.Component;
using UnityEngine;

namespace Module.fight.CardMgr
{
    public class ActionQueueUIManager
    {
        private readonly List<Transform> _uiActions;
        
        public ActionQueueUIManager(List<Transform> uiActions)
        {
            _uiActions = uiActions;
        }

        public void Init()
        {
            //暂无
        }
        
        //执行<打出卡牌>动画
        public void ExecuteCardUI(Transform transform, Transform _cardActionTf, Transform _cardDeckTf)
        {
            Transform executingCard = null;

            for (int i = 0; i < _cardActionTf.childCount; i++)
            {
                Transform child = _cardActionTf.GetChild(i);
                if (child.GetComponent<UI_BaseCardItem>() != null)
                {
                    executingCard = child;
                    break;
                }
            }

            if (executingCard == null) return;

            executingCard.SetParent(transform);
            executingCard.SetAsLastSibling();

            RectTransform rect = executingCard.GetComponent<RectTransform>();

            // 阶段 1：甩出卡牌 (抛物线 + 旋转 + 放大)
            Sequence seq = DOTween.Sequence();
            seq.Append(rect.DOMoveX(transform.position.x, 0.45f).SetEase(Ease.OutCirc));
            seq.Join(rect.DOMoveY(transform.position.y, 0.45f).SetEase(Ease.OutBack, 1.2f));
            seq.Join(rect.DOScale(Vector3.one * 1.5f, 0.45f).SetEase(Ease.OutQuad));
            seq.Join(rect.DORotate(new Vector3(0, 0, -8f), 0.45f).SetEase(Ease.OutQuad));

            // 阶段 2：悬停展示 (让玩家看清打出了什么)
            seq.AppendInterval(0.5f);

            // 阶段 3：爆发击出/消失 (急速收缩 + 回正)
            seq.Append(rect.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack));
            seq.Join(rect.DORotate(Vector3.zero, 0.2f));

            seq.OnComplete(() =>
            {
                executingCard.gameObject.SetActive(false);
                executingCard.SetParent(_cardDeckTf);

                // 重置状态，防止卡牌下次回到手牌时还是歪的/缩小的
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity;
            });
        }

        //更新卡牌队列背景UI
        public void UpdateCardQueueUI()
        {
            int maxCount =  GameApp.CardManager.CardActionQueue.MaxActionCount;
            for (int i = 0; i < _uiActions.Count; i++)
            {
                int currentIndex = i;
                bool shouldActive = currentIndex < maxCount;

                if (_uiActions[currentIndex].gameObject.activeSelf && !shouldActive)
                {
                    CanvasGroup cg = _uiActions[i].GetComponent<CanvasGroup>();
                    if (cg != null)
                    {
                        cg.DOKill();
                        cg.DOFade(0, 0.5f).onComplete = () =>
                        {
                            _uiActions[currentIndex].gameObject.SetActive(false);
                            cg.alpha = 1f;
                        };
                    }
                    else
                    {
                        _uiActions[currentIndex].gameObject.SetActive(false);
                    }

                }
            }
        }
        
        #region Move占位符
        //隐藏所有Move占位符
        public void HideAllMoveIndicators()
        {
            for (int i = 0; i < _uiActions.Count; i++)
            {
                Transform imgMove = _uiActions[i].Find("Img_move");
                if(imgMove != null) imgMove.gameObject.SetActive(false);
            }
        }
        
        //刷新Move占位符
        public void RefreshMoveIndicators()
        {
            CardAction[] actions = GameApp.CardManager.CardActionQueue.GetAction();

            for (int i = 0; i < _uiActions.Count; i++)
            {
                Transform imgMove = _uiActions[i].Find("Img_move");
                if (imgMove != null)
                {
                    bool isMove = i < actions.Length && actions[i].ActionType == CardActionType.MoveCard;

                    if (imgMove.gameObject.activeSelf != isMove)
                    {
                        imgMove.gameObject.SetActive(isMove);
                        
                        CanvasGroup cg = imgMove.GetComponent<CanvasGroup>();
                        if (cg != null && isMove)
                        {
                            cg.alpha = 0f;
                            cg.DOFade(1f, 0.2f);
                        }
                    }
                }
            }
        }
        #endregion

        #region 工具函数
        public void SetVisible(bool value)
        {
            for (int i = _uiActions.Count - 1; i >= 0; i--)
            {
                if (!_uiActions[i].gameObject.activeSelf)
                {
                    _uiActions[i].gameObject.SetActive(value);
                }
            }
        }
        #endregion
        
        #region 其他事件操作
        //刷新英雄行动点
        public void RefreshHeroActionPointUI()
        {
            var currentActions = GameApp.CardManager.CardActionQueue.GetAction();
            
            CardAction firstAction = currentActions.Length > 0 ? currentActions[0] : null;

            foreach (var hero in GameApp.EntityManager.GetAliveHeroes())
            {
                int previewGain = 0;

                if (firstAction != null && firstAction.Snapshot != null)
                {
                    if(firstAction.Snapshot.HeroActionPoints.TryGetValue(hero.InstanceID, out int baseActionPoint))
                    {
                        previewGain = hero.ActionPoint - baseActionPoint;
                    }
                }
                
                hero.HUD?.UpdateActionPoint(hero.ActionPoint, previewGain);
            }
        }
        #endregion
    }
}