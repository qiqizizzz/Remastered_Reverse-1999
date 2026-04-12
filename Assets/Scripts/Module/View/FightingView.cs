/*
* ┌────────────────────────────────────────────────────┐
* │  描    述: 战斗HUD(关卡信息、暂停等操作按钮、卡组等)                      
* │  类    名: FightingView.cs       
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────┘
*/

using Common.Defines;
using MVC.View;
using UnityEngine.UI;

namespace Module.View
{
    public class FightingView : BaseView
    {
        protected override void OnStart()
        {
            Find<Button>("OperationBtns/Btn_pause").onClick.AddListener(onPauseBtn);
        }

        private void onPauseBtn()
        {
            ApplyFunc(EventDefines.OpenPauseFightView);
        }
    }
}