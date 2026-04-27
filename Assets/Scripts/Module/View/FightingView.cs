/*
* ┌────────────────────────────────────────────────────┐
* │  描    述: 战斗HUD(关卡信息、暂停等操作按钮、卡组等)
* │  类    名: FightingView.cs
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Common;
using Common.Defines;
using Data.card;
using DG.Tweening;
using Module.Character;
using Module.fight.CardMgr;
using Module.fight.Component;
using MVC.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Module.View
{
    public class FightingView : BaseView
    {
        private HandCardUIManager m_handCardUIManager;
        private HandCardOperator m_handCardOperator;
        private ActionQueueUIManager m_actionQueueUIManager;
        private CardPoolManager m_cardPoolManager;

        [Header("关卡信息相关")]
        private Text _turnInfoText;

        [Header("队列区域相关")]
        private Transform _cardActionTf;
        private float _cardActionWidth = 550f;

        [Header("手牌区域相关")]
        private Transform _cardDeckTf;

        private Action m_refreshMoveIndicatorsHandler;

        #region 生命周期
        protected override void OnAwake()
        {
            _cardActionTf = Find<Transform>("CardAction");
            _cardDeckTf = Find<Transform>("CardDeck");
            _turnInfoText = Find<Text>("FightDetail/Round/Txt_turnNum");

            m_cardPoolManager = new CardPoolManager(_cardDeckTf);
            m_handCardUIManager = new HandCardUIManager(_cardDeckTf, m_cardPoolManager);

            #region 队列UI
            List<Transform> m_UIActions = new List<Transform>();
            m_UIActions.Add(Find<Transform>("CardAction/queue_1"));
            m_UIActions.Add(Find<Transform>("CardAction/queue_2"));
            m_UIActions.Add(Find<Transform>("CardAction/queue_3"));
            m_UIActions.Add(Find<Transform>("CardAction/queue_4"));
            m_actionQueueUIManager = new ActionQueueUIManager(m_UIActions);
            #endregion
        }

        protected override void OnEnable()
        {
            Find<Button>("OperationBtns/Btn_pause").onClick.AddListener(onPauseBtn);
            Find<Button>("CardAction/Btn_Undo").onClick.AddListener(onUndoBtn);

            GameApp.MessageCenter.AddEvent(EventDefines.OnPlayerTurnOutput, onHideAllHands);
            GameApp.MessageCenter.AddEvent(EventDefines.OnCardExecuteUI, onCardExecuteUI);
            GameApp.MessageCenter.AddEvent(EventDefines.OnRemoveDiedCharacterCard, onRemoveDiedCharacterCardsUI);
            GameApp.MessageCenter.AddEvent(EventDefines.OnHandCardChanged, onHandCardChanged);
        }

        protected override void OnStart()
        {
            Controller.RegisterFunc(EventDefines.UpdateHandCards, onUpdateHandCards);
            Controller.RegisterFunc(EventDefines.ExitLevel, onExitLevel);

            m_actionQueueUIManager.Init();
            m_cardPoolManager.Init();

            m_handCardOperator = new HandCardOperator(
                m_handCardUIManager,
                GameApp.CardManager.CardActionQueue,
                _cardActionTf,
                _cardDeckTf,
                _cardActionWidth);
            m_handCardOperator.Init();

            m_refreshMoveIndicatorsHandler = () => m_actionQueueUIManager.RefreshMoveIndicators();
            m_handCardOperator.OnRefreshMoveIndicators += m_refreshMoveIndicatorsHandler;
            m_handCardOperator.OnQueueFull += onQueueFull;
        }

        protected override void OnDisable()
        {
            GameApp.MessageCenter.RemoveEvent(EventDefines.OnPlayerTurnOutput, onHideAllHands);
            GameApp.MessageCenter.RemoveEvent(EventDefines.OnCardExecuteUI, onCardExecuteUI);
            GameApp.MessageCenter.RemoveEvent(EventDefines.OnRemoveDiedCharacterCard, onRemoveDiedCharacterCardsUI);
            GameApp.MessageCenter.RemoveEvent(EventDefines.OnHandCardChanged, onHandCardChanged);
        }

        protected override void OnDestroy()
        {
            Controller.UnRegisterFunc(EventDefines.UpdateHandCards, onUpdateHandCards);
            Controller.UnRegisterFunc(EventDefines.ExitLevel, onExitLevel);

            m_cardPoolManager.UnLoadAll();

            if (m_handCardOperator != null)
            {
                m_handCardOperator.OnRefreshMoveIndicators -= m_refreshMoveIndicatorsHandler;
                m_handCardOperator.OnQueueFull -= onQueueFull;
            }
        }
        #endregion

        public override void Open(params object[] args)
        {
            SetVisible(true);

            m_actionQueueUIManager.SetVisible(true);
        }
        
        private void onHideAllHands(System.Object args)
        {
            m_handCardUIManager.HideAllHands(args);
            m_actionQueueUIManager.HideAllMoveIndicators();
        }

        private void onHandCardChanged(System.Object args = null)
        {
            onUpdateHandCards(GameApp.CardManager.GetHandCards());
        }
        
        private void onUpdateLevelInfo(params object[] args)
        {
            //TODO:更新轮次,后面再说吧。。
        }

        private void onUpdateHandCards(params object[] args)
        {
            List<BattleCardData> newCards = args[0] as List<BattleCardData>;
            bool isUndo = args.Length > 1 && args[1] is true;

            if (newCards == null) return;

            m_handCardUIManager.UpdateCardsUI(newCards, isUndo, () =>
            {
                if (!isUndo)
                    processPostHandUpdate();
            });
        }

        /// <summary>
        /// 手牌布局稳定后的后置处理：合成检查 → 补牌
        /// </summary>
        private void processPostHandUpdate()
        {
            m_handCardOperator.CheckAndTriggerComposite(() =>
            {
                int normalCount = GameApp.CardManager.GetNormalHandCardCount();
                if (normalCount < GameApp.CardManager.mMaxHandCardCount)
                {
                    int needCount = GameApp.CardManager.mMaxHandCardCount - normalCount;
                    int beforeCount = GameApp.CardManager.GetHandCards().Count;

                    GameApp.CardManager.DrawCard(needCount);

                    if (GameApp.CardManager.GetHandCards().Count > beforeCount)
                        onUpdateHandCards(GameApp.CardManager.GetHandCards());
                }
            });
        }

        private void onCardExecuteUI(System.Object args = null)
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

        private void onRemoveDiedCharacterCardsUI(System.Object args = null)
        {
            m_handCardUIManager.RemoveDiedCharacterCard(args);
            m_actionQueueUIManager.UpdateCardQueueUI();
        }

        #region 按钮回调
        private void onPauseBtn()
        {
            ApplyFunc(EventDefines.OpenPauseFightView);
        }

        private void onUndoBtn()
        {
            CardAction lastAction = GameApp.CardManager.CardActionQueue.UndoLastAction();
            if (lastAction == null) return;

            GameApp.CardManager.RestoreSnapshot(lastAction.Snapshot);

            if (lastAction.ActionType == CardActionType.PlayCard)
            {
                m_handCardOperator.UndoLastPlayCard();
            }

            // 更新玩家行动点UI
            foreach (var hero in GameApp.EntityManager.GetAliveHeroes())
            {
                hero.HUD?.UpdateActionPoint(hero.ActionPoint);
            }

            // 统一刷新移动占位符 UI
            m_actionQueueUIManager.RefreshMoveIndicators();

            // 全量重建当前手牌 UI
            onUpdateHandCards(GameApp.CardManager.GetHandCards(), true);
        }

        private void onExitLevel(params object[] args)
        {
            // 注意:所有离开关卡的行为都需要经过这个事件,不然动画UI等效果会出错

            m_handCardOperator.Clear();
            m_handCardUIManager.Clear();
            m_cardPoolManager.RecycleAllCards();
            onHideAllHands(null);
        }
        #endregion
        
        private void onQueueFull()
        {
            DOVirtual.DelayedCall(2f, () =>
            {
                GameApp.MessageCenter.PostEvent(EventDefines.OnPlayerTurnOutput);
            });
        }
    }
}
