/*
* ┌──────────────────────────────────┐
* │  描    述: 行动队列UI管理器                      
* │  类    名: ActionQueueUIManager.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using DG.Tweening;
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
    }
}