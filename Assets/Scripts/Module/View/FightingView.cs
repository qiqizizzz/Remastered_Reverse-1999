/*
* ┌────────────────────────────────────────────────────┐
* │  描    述: 战斗HUD(关卡信息、暂停等操作按钮、卡组等)
* │  类    名: FightingView.cs
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Common.Defines;
using DG.Tweening;
using Module.Character;
using Module.fight.CardMgr;
using Module.fight.Core.Entities;
using MVC.View;
using UnityEngine;
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
        //private float _cardActionWidth = 550f;

        [Header("手牌区域相关")]
        private Transform _cardDeckTf;

        private Action m_refreshMoveIndicatorsHandler;
        private Action<object> m_onUpdateHandCardsHandler;

        #region 生命周期
        protected override void OnAwake()
        {
            _cardActionTf = Find<Transform>("CardAction");
            _cardDeckTf = Find<Transform>("CardDeck");
            _turnInfoText = Find<Text>("FightDetail/Round/Txt_turnNum");

            m_cardPoolManager = new CardPoolManager(_cardDeckTf);
            m_handCardUIManager = new HandCardUIManager(_cardDeckTf, _cardActionTf, m_cardPoolManager);

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
            GameApp.MessageCenter.AddEvent(EventDefines.OnCardExecuteUI, onExecuteCardUI);
            GameApp.MessageCenter.AddEvent(EventDefines.OnRemoveDiedCharacterCard, onRemoveDiedCharacterCardsUI);
            GameApp.MessageCenter.AddEvent(EventDefines.OnHandCardChanged, onHandCardChanged);
        }

        protected override void OnStart()
        {
            m_onUpdateHandCardsHandler = args => onUpdateHandCards(new object[] { args });
            GameApp.MessageCenter.AddEvent(EventDefines.UpdateHandCards, m_onUpdateHandCardsHandler);
            Controller.RegisterFunc(EventDefines.ExitLevel, onExitLevel);

            m_actionQueueUIManager.Init();
            m_cardPoolManager.Init();

            m_handCardOperator = new HandCardOperator(m_handCardUIManager, GameApp.CardManager.CardActionQueue);
            m_handCardOperator.Init();

            m_refreshMoveIndicatorsHandler = () => m_actionQueueUIManager.RefreshMoveIndicators();
            m_handCardOperator.OnRefreshMoveIndicators += m_refreshMoveIndicatorsHandler;
            m_handCardOperator.OnQueueFull += onQueueFull;
            m_handCardOperator.OnRefreshActionPointUI +=  m_actionQueueUIManager.RefreshHeroActionPointUI;
        }

        protected override void OnDisable()
        {
            GameApp.MessageCenter.RemoveEvent(EventDefines.OnPlayerTurnOutput, onHideAllHands);
            GameApp.MessageCenter.RemoveEvent(EventDefines.OnCardExecuteUI, onExecuteCardUI);
            GameApp.MessageCenter.RemoveEvent(EventDefines.OnRemoveDiedCharacterCard, onRemoveDiedCharacterCardsUI);
            GameApp.MessageCenter.RemoveEvent(EventDefines.OnHandCardChanged, onHandCardChanged);
        }

        protected override void OnDestroy()
        {
            if (m_onUpdateHandCardsHandler != null)
                GameApp.MessageCenter.RemoveEvent(EventDefines.UpdateHandCards, m_onUpdateHandCardsHandler);
            Controller.UnRegisterFunc(EventDefines.ExitLevel, onExitLevel);

            m_cardPoolManager.UnLoadAll();

            if (m_handCardOperator != null)
            {
                m_handCardOperator.OnRefreshMoveIndicators -= m_refreshMoveIndicatorsHandler;
                m_handCardOperator.OnQueueFull -= onQueueFull;
                m_handCardOperator.OnRefreshActionPointUI -=  m_actionQueueUIManager.RefreshHeroActionPointUI;
            }
        }
        #endregion

        public override void Open(params object[] args)
        {
            SetVisible(true);

            m_actionQueueUIManager.SetVisible(true);
        }

        #region UI事件
        #region 手牌事件
        private void onHideAllHands(System.Object args)
        {
            m_handCardUIManager.HideAllHands(args);
            m_actionQueueUIManager.HideAllMoveIndicators();
        }
        private void onHandCardChanged(System.Object args = null)
        {
            onUpdateHandCards(GameApp.CardManager.GetHandCards());
        }
        private void onUpdateHandCards(params object[] args)
        {
            List<CardEntity> newCards = args[0] as List<CardEntity>;
            bool isUndo = args.Length > 1 && args[1] is true;

            if (newCards == null) return;

            m_handCardUIManager.UpdateCardsUI(newCards, isUndo, null);
        }
        

        #endregion

        #region 队列事件
        private void onExecuteCardUI(System.Object args = null)
        {
            m_actionQueueUIManager.ExecuteCardUI(transform, _cardActionTf, _cardDeckTf);
        }
        
        private void onQueueFull()
        {
            DOVirtual.DelayedCall(2f, () =>
            {
                GameApp.MessageCenter.PostEvent(EventDefines.OnPlayerTurnOutput);
            });
        }
        #endregion
        
        private void onRemoveDiedCharacterCardsUI(System.Object args = null)
        {
            m_handCardUIManager.RemoveDiedCharacterCard(args);
            m_actionQueueUIManager.UpdateCardQueueUI();
        }
        
        private void onUpdateLevelInfo(params object[] args)
        {
            //TODO:更新轮次,后面再说吧。。
        }
        #endregion
        
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

            m_actionQueueUIManager.RefreshHeroActionPointUI();
            
            // 更新玩家行动点UI
            /*foreach (var hero in GameApp.EntityManager.GetAliveHeroes())
            {
                
                //hero.HUD?.UpdateActionPoint(hero.ActionPoint);
            }*/

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
    }
}
