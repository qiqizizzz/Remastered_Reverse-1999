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
        private List<Transform> m_UIActions;
        private CardActionQueue _cardActionQueue;
        
        public ActionQueueUIManager(List<Transform> uiActions)
        {
            m_UIActions = uiActions;
        }

        public void Init()
        {
            _cardActionQueue = GameApp.CardManager.CardActionQueue;
        }

        public void SetVisible(bool value)
        {
            for (int i = m_UIActions.Count - 1; i >= 0; i--)
            {
                if (!m_UIActions[i].gameObject.activeSelf)
                {
                    m_UIActions[i].gameObject.SetActive(value);
                }
            }
        }
        
        public void HideAllMoveIndicators()
        {
            for (int i = 0; i < m_UIActions.Count; i++)
            {
                Transform imgMove = m_UIActions[i].Find("Img_move");
                if(imgMove != null) imgMove.gameObject.SetActive(false);
            }
        }

        public void UpdateCardQueueUI()
        {
            int maxCount = _cardActionQueue.MaxActionCount;
            for (int i = 0; i < m_UIActions.Count; i++)
            {
                int currentIndex = i;
                bool shouldActive = currentIndex < maxCount;

                if (m_UIActions[currentIndex].gameObject.activeSelf && !shouldActive)
                {
                    CanvasGroup cg = m_UIActions[i].GetComponent<CanvasGroup>();
                    if (cg != null)
                    {
                        cg.DOKill();
                        cg.DOFade(0, 0.5f).onComplete = () =>
                        {
                            m_UIActions[currentIndex].gameObject.SetActive(false);
                            cg.alpha = 1f;
                        };
                    }
                    else
                    {
                        m_UIActions[currentIndex].gameObject.SetActive(false);
                    }

                }
            }
        }

        //刷新Move占位符
        public void RefreshMoveIndicators()
        {
            CardAction[] actions = _cardActionQueue.GetAction();

            for (int i = 0; i < m_UIActions.Count; i++)
            {
                Transform imgMove = m_UIActions[i].Find("Img_move");
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

        public void CardExecuteUI(Transform transform, Transform _cardActionTf, Transform _cardDeckTf)
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
    }
}