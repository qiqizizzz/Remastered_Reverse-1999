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
        private const int MATCH_SUCCESS_WAIT_TIME = 5;

        private TextMeshProUGUI _txt;
        private BattleNetworkController _battleNetwork;
        private PvpPrepareData _pendingPrepareData;
        private int _countdownToken;
        private bool _isMatchSuccessCountdown;//成功匹配
        private int _elapsedWaitTime;//等待时间

        protected override void OnAwake()
        {
            _txt = Find<TextMeshProUGUI>("txt");
            _battleNetwork = new BattleNetworkController();

            Find<Button>("Btn_cancel").onClick.AddListener(onCancelBtn);
        }

        public override void Open(params object[] args)
        {
            _battleNetwork.InitPvpPrepare();
            _pendingPrepareData = null;
            _isMatchSuccessCountdown = false;
            _elapsedWaitTime = 0;
            int token = ++_countdownToken;
            
            _txt.text = $"匹配中... (已等待{_elapsedWaitTime}s)";
            GameApp.TimerManager.Register(1f, () => updateWaitTimeCountdown(token));
            
            //注册事件
            GameApp.MessageCenter.AddEvent(EventDefines.OnPvpMatchSuccess, onPvpMatchSuccess);
            GameApp.MessageCenter.AddEvent(EventDefines.OnPvpMatchFailed, onPvpMatchFailed);
            _battleNetwork.SendJoinPvp();
        }

        public override void Close(params object[] args)
        {
            GameApp.MessageCenter.RemoveEvent(EventDefines.OnPvpMatchSuccess, onPvpMatchSuccess);
            GameApp.MessageCenter.RemoveEvent(EventDefines.OnPvpMatchFailed, onPvpMatchFailed);
            _countdownToken++;
            _isMatchSuccessCountdown = false;
            _pendingPrepareData = null;
            _battleNetwork.UnInitPvpPrepare();
            base.Close(args);
        }

        // 处理PVP匹配成功
        private void onPvpMatchSuccess(object arg)
        {
            _pendingPrepareData = arg as PvpPrepareData;
            _isMatchSuccessCountdown = true;
            int token = ++_countdownToken;
            _txt.text = $"匹配成功！{MATCH_SUCCESS_WAIT_TIME}s后进入角色界面...";
            GameApp.TimerManager.Register(1f, () => updateMatchSuccessCountdown(token, MATCH_SUCCESS_WAIT_TIME - 1));
        }
        
        // 更新匹配等待计时
        private void updateWaitTimeCountdown(int token)
        {
            if (_isMatchSuccessCountdown || token != _countdownToken) return;

            _elapsedWaitTime++;
            _txt.text = $"匹配中... (已等待{_elapsedWaitTime}s)";
            GameApp.TimerManager.Register(1f, () => updateWaitTimeCountdown(token));
        }

        // 更新匹配成功倒计时
        private void updateMatchSuccessCountdown(int token, int remainingTime)
        {
            if (!_isMatchSuccessCountdown || token != _countdownToken) return;

            if (remainingTime > 0)
            {
                _txt.text = $"匹配成功！{remainingTime}s后进入角色界面...";
                GameApp.TimerManager.Register(1f, () => updateMatchSuccessCountdown(token, remainingTime - 1));
                return;
            }

            _isMatchSuccessCountdown = false;
            PvpPrepareData prepareData = _pendingPrepareData;
            _pendingPrepareData = null;
            GameApp.ViewManager.Close(ViewType.MatchmakingView);
            GameApp.ViewManager.Open(ViewType.PrepareFightView, prepareData);
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