/*
* ┌──────────────────────────────────┐
* │  描    述: 匹配界面                      
* │  类    名: MatchmakingView.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common.Defines;
using Module.fight.Network;
using Module.Matchmaking;
using MVC;
using MVC.View;
using TMPro;
using UnityEngine.UI;

namespace Module.View
{
    public class MatchmakingView : BaseView
    {
        private TextMeshProUGUI _txt;
        private BattleNetworkController _battleNetwork;

        protected override void OnAwake()
        {
            _txt = Find<TextMeshProUGUI>("txt");
            _battleNetwork = new BattleNetworkController();
            _battleNetwork.Init();

            Find<Button>("Btn_cancel").onClick.AddListener(onCancelBtn);
        }

        public override void Open(params object[] args)
        {
            base.Open(args);
            _txt.text = "匹配中...";
            GameApp.MessageCenter.AddEvent(EventDefines.OnPvpMatchSuccess, onPvpMatchSuccess);
            GameApp.MessageCenter.AddEvent(EventDefines.OnPvpMatchFailed, onPvpMatchFailed);
            _battleNetwork.SendJoinPvp();
        }

        public override void Close(params object[] args)
        {
            GameApp.MessageCenter.RemoveEvent(EventDefines.OnPvpMatchSuccess, onPvpMatchSuccess);
            GameApp.MessageCenter.RemoveEvent(EventDefines.OnPvpMatchFailed, onPvpMatchFailed);
            base.Close(args);
        }

        // 处理PVP匹配成功
        private void onPvpMatchSuccess(object arg)
        {
            _txt.text = "匹配成功！";
            GameApp.ViewManager.Close(ViewType.MatchmakingView);
            GameApp.ViewManager.Open(ViewType.PrepareFightView, arg as PvpPrepareData);
        }

        // 处理PVP匹配失败
        private void onPvpMatchFailed(object arg)
        {
            _txt.text = arg as string ?? "匹配失败";
        }

        private void onCancelBtn()
        {
            _battleNetwork.SendLeavePvp();
            GameApp.ViewManager.NavigateBack();//返回上一级
        }
    }
}