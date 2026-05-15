/*
* ┌──────────────────────────────────┐
* │  描    述: 战斗网络控制器，负责战斗协议的发送与响应接收
* │  类    名: BattleNetworkController.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common.Defines;
using GameProtocol;
using Network;
using Common;
using Module.Matchmaking;
using UnityEngine;

namespace Module.fight.Network
{
    public class BattleNetworkController
    {
        private bool _isBattleInitialized;
        private bool _isPvpPrepareInitialized;

        // 注册战斗内协议响应
        public void Init()
        {
            if (_isBattleInitialized) return;
            _isBattleInitialized = true;

            GameApp.NetworkManager.AddMessageHandler(ActionCode.EnterPve, onEnterPveResponse);
            GameApp.NetworkManager.AddMessageHandler(ActionCode.PlayCard, onPlayCardResponse);
            GameApp.NetworkManager.AddMessageHandler(ActionCode.EndTurn, onEndTurnResponse);
            GameApp.NetworkManager.AddMessageHandler(ActionCode.MoveCard, onMoveCardResponse);
            GameApp.NetworkManager.AddMessageHandler(ActionCode.UnDoAction, onUndoResponse);
            GameApp.NetworkManager.AddMessageHandler(ActionCode.RequestBattleState, onRequestBattleStateResponse);
        }

        // 反注册战斗内协议响应
        public void UnInit()
        {
            if (!_isBattleInitialized) return;
            _isBattleInitialized = false;

            GameApp.NetworkManager.RemoveMessageHandler(ActionCode.EnterPve, onEnterPveResponse);
            GameApp.NetworkManager.RemoveMessageHandler(ActionCode.PlayCard, onPlayCardResponse);
            GameApp.NetworkManager.RemoveMessageHandler(ActionCode.EndTurn, onEndTurnResponse);
            GameApp.NetworkManager.RemoveMessageHandler(ActionCode.MoveCard, onMoveCardResponse);
            GameApp.NetworkManager.RemoveMessageHandler(ActionCode.UnDoAction, onUndoResponse);
            GameApp.NetworkManager.RemoveMessageHandler(ActionCode.RequestBattleState, onRequestBattleStateResponse);
        }

        // 注册PvP准备阶段协议响应
        public void InitPvpPrepare()
        {
            if (_isPvpPrepareInitialized) return;
            _isPvpPrepareInitialized = true;

            GameApp.NetworkManager.AddMessageHandler(ActionCode.JoinPvP, onJoinPvpResponse);
            GameApp.NetworkManager.AddMessageHandler(ActionCode.LeavePvP, onLeavePvpResponse);
            GameApp.NetworkManager.AddMessageHandler(ActionCode.SubmitPvPteam, onSubmitPvpTeamResponse);
        }

        // 反注册PvP准备阶段协议响应
        public void UnInitPvpPrepare()
        {
            if (!_isPvpPrepareInitialized) return;
            _isPvpPrepareInitialized = false;

            GameApp.NetworkManager.RemoveMessageHandler(ActionCode.JoinPvP, onJoinPvpResponse);
            GameApp.NetworkManager.RemoveMessageHandler(ActionCode.LeavePvP, onLeavePvpResponse);
            GameApp.NetworkManager.RemoveMessageHandler(ActionCode.SubmitPvPteam, onSubmitPvpTeamResponse);
        }

        #region 发送请求
        // 发送进入PVE请求
        public void SendEnterPve(int levelId)
        {
            var pack = new MainPack
            {
                RequestCode = RequestCode.Battle,
                ActionCode = ActionCode.EnterPve,
                BattlePack = new BattlePack { LevelId = levelId }
            };
            GameApp.NetworkManager.Send(pack);
        }
        
        // 发送进入PVP匹配队列请求
        public void SendJoinPvp()
        {
            var pack = new MainPack
            {
                RequestCode = RequestCode.Battle,
                ActionCode = ActionCode.JoinPvP
            };
            GameApp.NetworkManager.Send(pack);
        }

        // 发送离开PVP匹配队列或准备房间请求
        public void SendLeavePvp()
        {
            var pack = new MainPack
            {
                RequestCode = RequestCode.Battle,
                ActionCode = ActionCode.LeavePvP
            };
            GameApp.NetworkManager.Send(pack);
        }

        // 发送PVP阵容提交请求
        public void SendSubmitPvpTeam(System.Collections.Generic.List<int> heroIds)
        {
            var battlePack = new BattlePack
            {
                MatchId = GameApp.PvpSession.MatchId,
                PlayerId = getCurrentPlayerId()
            };
            battlePack.HeroIds.AddRange(heroIds);

            var pack = new MainPack
            {
                RequestCode = RequestCode.Battle,
                ActionCode = ActionCode.SubmitPvPteam,
                BattlePack = battlePack
            };
            GameApp.NetworkManager.Send(pack);
        }

        // 发送出牌请求
        public void SendPlayCard(int cardInstanceId, int targetInstanceId, int handIndex)
        {
            var pack = new MainPack
            {
                RequestCode = RequestCode.Battle,
                ActionCode = ActionCode.PlayCard,
                BattlePack = new BattlePack
                {
                    PlayerId = getCurrentPlayerId(),
                    CardInstanceId = cardInstanceId,
                    TargetEntityId = targetInstanceId,
                    SourceSlotIndex = handIndex
                }
            };
            GameApp.NetworkManager.Send(pack);
        }

        // 发送结束回合请求
        public void SendEndTurn()
        {
            var pack = new MainPack
            {
                RequestCode = RequestCode.Battle,
                ActionCode = ActionCode.EndTurn,
                BattlePack = new BattlePack { PlayerId = getCurrentPlayerId() }
            };
            GameApp.NetworkManager.Send(pack);
        }

        // 发送移动卡牌请求
        public void SendMoveCard(int fromIndex, int toIndex)
        {
            var pack = new MainPack
            {
                RequestCode = RequestCode.Battle,
                ActionCode = ActionCode.MoveCard,
                BattlePack = new BattlePack
                {
                    PlayerId = getCurrentPlayerId(),
                    SourceSlotIndex = fromIndex,
                    TargetSlotIndex = toIndex
                }
            };
            GameApp.NetworkManager.Send(pack);
        }

        // 发送撤销请求
        public void SendUndo()
        {
            var pack = new MainPack
            {
                RequestCode = RequestCode.Battle,
                ActionCode = ActionCode.UnDoAction,
                BattlePack = new BattlePack { PlayerId = getCurrentPlayerId() }
            };
            GameApp.NetworkManager.Send(pack);
        }

        // 发送请求完整状态（断线重连）
        public void SendRequestBattleState()
        {
            var pack = new MainPack
            {
                RequestCode = RequestCode.Battle,
                ActionCode = ActionCode.RequestBattleState
            };
            GameApp.NetworkManager.Send(pack);
        }
        
        #endregion

        #region 响应处理
        // 进入PVE响应
        private void onEnterPveResponse(MainPack pack)
        {
            if (!checkResponse(pack, ActionCode.EnterPve)) return;
            broadcastEvents(pack);
        }

        // 出牌响应
        private void onPlayCardResponse(MainPack pack)
        {
            if (!checkResponse(pack, ActionCode.PlayCard)) return;
            broadcastEvents(pack);
        }

        // 结束回合响应
        private void onEndTurnResponse(MainPack pack)
        {
            if (!checkResponse(pack, ActionCode.EndTurn)) return;
            broadcastEvents(pack);
        }

        // 移动卡牌响应
        private void onMoveCardResponse(MainPack pack)
        {
            if (!checkResponse(pack, ActionCode.MoveCard)) return;
            broadcastEvents(pack);
        }

        // 撤销响应
        private void onUndoResponse(MainPack pack)
        {
            if (!checkResponse(pack, ActionCode.UnDoAction)) return;
            if (pack.BattlePack != null)
                GameApp.MessageCenter.PostEvent(EventDefines.OnBattleServerResponse, new object[] { pack.BattlePack, true });
        }

        // 请求完整状态响应
        private void onRequestBattleStateResponse(MainPack pack)
        {
            if (!checkResponse(pack, ActionCode.RequestBattleState)) return;
            broadcastEvents(pack);
        }

        // PVP匹配响应
        private void onJoinPvpResponse(MainPack pack)
        {
            if (!checkResponse(pack, ActionCode.JoinPvP))
            {
                GameApp.MessageCenter.PostEvent(EventDefines.OnPvpMatchFailed, pack.StrMsg);
                return;
            }

            if (pack.BattlePack == null || !pack.BattlePack.IsMatchSuccess) return;

            GameApp.PvpSession.SetPrepareRoom(pack.BattlePack.MatchId, pack.BattlePack.PlayerId);
            GameApp.MessageCenter.PostEvent(EventDefines.OnPvpMatchSuccess,
                new PvpPrepareData(pack.BattlePack.MatchId, pack.BattlePack.PlayerId));
        }

        // 离开PVP响应
        private void onLeavePvpResponse(MainPack pack)
        {
            if (!checkResponse(pack, ActionCode.LeavePvP)) return;
            GameApp.PvpSession.Clear();
        }

        // PVP阵容提交响应
        private void onSubmitPvpTeamResponse(MainPack pack)
        {
            if (!checkResponse(pack, ActionCode.SubmitPvPteam)) return;
            if (pack.BattlePack == null) return;

            if (!pack.BattlePack.IsTeamReady)
            {
                GameApp.MessageCenter.PostEvent(EventDefines.OnPvpTeamWaiting, pack.BattlePack);
                return;
            }

            GameApp.MessageCenter.PostEvent(EventDefines.OnPvpBattleStart,
                new PvpBattleStartData(pack.BattlePack.PlayerId, pack.BattlePack));
        }
        #endregion

        // 获取当前操作者编号
        private int getCurrentPlayerId()
        {
            if (GameApp.PvpSession != null && GameApp.PvpSession.IsInPvp)
                return GameApp.PvpSession.CurrentPlayerId;

            return 1;
        }

        // 检查响应是否成功
        private bool checkResponse(MainPack pack, ActionCode actionCode)
        {
            if (pack.ReturnCode == ReturnCode.Succeed) return true;

            QLog.Warning($"[BattleNetwork] {actionCode} 失败: {pack.StrMsg}");
            return false;
        }

        // 将服务端返回的战斗事件广播给表现层
        private void broadcastEvents(MainPack pack)
        {
            if (pack.BattlePack == null) return;
            GameApp.MessageCenter.PostEvent(EventDefines.OnBattleServerResponse, pack.BattlePack);
        }
    }
}
