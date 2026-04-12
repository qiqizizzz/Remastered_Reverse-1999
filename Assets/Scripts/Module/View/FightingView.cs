/*
* ┌────────────────────────────────────────────────────┐
* │  描    述: 战斗HUD(关卡信息、暂停等操作按钮、卡组等)                      
* │  类    名: FightingView.cs       
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────┘
*/

using Common.Defines;
using MVC.View;
using UnityEngine;
using UnityEngine.UI;

namespace Module.View
{
    public class FightingView : BaseView
    {
        private Transform cardDeckTf;

        protected override void OnAwake()
        {
            cardDeckTf = Find<Transform>("CardDeck");
        }

        protected override void OnStart()
        {
            Find<Button>("OperationBtns/Btn_pause").onClick.AddListener(onPauseBtn);
            
            Controller.RegisterFunc(EventDefines.UpdateHandCards, onUpdateHandCards);
        }
        
        private void onPauseBtn()
        {
            ApplyFunc(EventDefines.OpenPauseFightView);
        }

        private void onUpdateHandCards(params object[] args)
        {
            //args[0] 手牌列表
            
            //从左侧开始依次生成卡牌预制体，放在cardDeckTf下(根据数量生成,可能还有没打完的卡牌)
            //这个应该只是增加卡牌的更新UI函数,后面拖动卡牌/合成卡牌/发牌还有另外的逻辑吧，这应该怎么做啊？
        }
    }
}