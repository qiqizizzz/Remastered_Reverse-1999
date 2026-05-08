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
using Module.fight.Component;
using Module.fight.Core.Commands;
using Module.fight.Core.Entities;
using MVC.View;
using UnityEngine;
using UnityEngine.UI;

namespace Module.View
{
    public class FightingView : BaseView
    {
        private HandCardUIManager _handCardUIManager;
        private HandCardOperator _handCardOperator;
        private ActionQueueUIManager _actionQueueUIManager;
        private CardPoolManager _cardPoolManager;
        


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

            _cardPoolManager = new CardPoolManager(_cardDeckTf);
            _handCardUIManager = new HandCardUIManager(_cardDeckTf, _cardActionTf, _cardPoolManager);

            #region 队列UI
            List<Transform> m_UIActions = new List<Transform>();
            m_UIActions.Add(Find<Transform>("CardAction/queue_1"));
            m_UIActions.Add(Find<Transform>("CardAction/queue_2"));
            m_UIActions.Add(Find<Transform>("CardAction/queue_3"));
            m_UIActions.Add(Find<Transform>("CardAction/queue_4"));
            _actionQueueUIManager = new ActionQueueUIManager(m_UIActions);
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

            _actionQueueUIManager.Init();
            _cardPoolManager.Init();

            _handCardOperator =
                new HandCardOperator(_handCardUIManager, GameApp.CardManager.CardActionQueue);
            _handCardOperator.Init();

            m_refreshMoveIndicatorsHandler = () => _actionQueueUIManager.RefreshMoveIndicators();
            _handCardOperator.OnRefreshMoveIndicators += m_refreshMoveIndicatorsHandler;
            _handCardOperator.OnQueueFull += onQueueFull;
            _handCardOperator.OnRefreshActionPointUI +=  _actionQueueUIManager.RefreshHeroActionPointUI;
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

            _cardPoolManager.UnLoadAll();

            if (_handCardOperator != null)
            {
                _handCardOperator.OnRefreshMoveIndicators -= m_refreshMoveIndicatorsHandler;
                _handCardOperator.OnQueueFull -= onQueueFull;
                _handCardOperator.OnRefreshActionPointUI -=  _actionQueueUIManager.RefreshHeroActionPointUI;
            }
        }
        #endregion

        public override void Open(params object[] args)
        {
            SetVisible(true);

            _actionQueueUIManager.SetVisible(true);
            _cardPoolManager.TryNotifyReady();
        }

        #region UI事件
        #region 手牌事件
        private void onHideAllHands(System.Object args)
        {
            _handCardUIManager.HideAllHands(args);
            _actionQueueUIManager.HideAllMoveIndicators();
            
            // 回收行动队列区域的所有卡牌到对象池
            for (int i = _cardActionTf.childCount - 1; i >= 0; i--)
            {
                var cardItem = _cardActionTf.GetChild(i).GetComponent<Module.fight.Component.UI_BaseCardItem>();
                if (cardItem != null)
                    _cardPoolManager.RecycleCard(cardItem);
            }
            _handCardOperator.ClearActionStack();
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

            _handCardUIManager.UpdateCardsUI(newCards, isUndo, null);
        }
        

        #endregion

        #region 队列事件
        private void onExecuteCardUI(System.Object args = null)
        {
            _actionQueueUIManager.ExecuteCardUI(transform, _cardActionTf, _cardDeckTf);
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
            _handCardUIManager.RemoveDiedCharacterCard(args);
            _actionQueueUIManager.UpdateCardQueueUI();
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
            _handCardOperator.UndoLastPlayCard();

            _actionQueueUIManager.RefreshHeroActionPointUI();
            _actionQueueUIManager.RefreshMoveIndicators();
        }

        private void onExitLevel(params object[] args)
        {
            _handCardOperator.Clear();
            _handCardUIManager.Clear();
            _cardPoolManager.RecycleAllCards();
        }
        #endregion
    }
}
