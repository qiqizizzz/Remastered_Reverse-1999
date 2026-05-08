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
using UnityEngine;

namespace Module.fight.Network
{
    public class BattleNetworkController
    {
        private bool _isInitialized;

        public void Init()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            GameApp.NetworkManager.AddMessageHandler(ActionCode.EnterPve, onEnterPveResponse);
            GameApp.NetworkManager.AddMessageHandler(ActionCode.PlayCard, onPlayCardResponse);
            GameApp.NetworkManager.AddMessageHandler(ActionCode.EndTurn, onEndTurnResponse);
            GameApp.NetworkManager.AddMessageHandler(ActionCode.MoveCard, onMoveCardResponse);
            GameApp.NetworkManager.AddMessageHandler(ActionCode.UnDoAction, onUndoResponse);
            GameApp.NetworkManager.AddMessageHandler(ActionCode.RequestBattleState, onRequestBattleStateResponse);
        }

        public void UnInit()
        {
            if (!_isInitialized) return;
            _isInitialized = false;

            GameApp.NetworkManager.RemoveMessageHandler(ActionCode.EnterPve, onEnterPveResponse);
            GameApp.NetworkManager.RemoveMessageHandler(ActionCode.PlayCard, onPlayCardResponse);
            GameApp.NetworkManager.RemoveMessageHandler(ActionCode.EndTurn, onEndTurnResponse);
            GameApp.NetworkManager.RemoveMessageHandler(ActionCode.MoveCard, onMoveCardResponse);
            GameApp.NetworkManager.RemoveMessageHandler(ActionCode.UnDoAction, onUndoResponse);
            GameApp.NetworkManager.RemoveMessageHandler(ActionCode.RequestBattleState, onRequestBattleStateResponse);
        }

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

        // 发送出牌请求
        public void SendPlayCard(int cardInstanceId, int targetInstanceId)
        {
            var pack = new MainPack
            {
                RequestCode = RequestCode.Battle,
                ActionCode = ActionCode.PlayCard,
                BattlePack = new BattlePack
                {
                    CardInstanceId = cardInstanceId,
                    TargetEntityId = targetInstanceId
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
                ActionCode = ActionCode.EndTurn
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
                ActionCode = ActionCode.UnDoAction
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

        // ==================== 响应处理 ====================
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
            broadcastEvents(pack);
        }

        // 请求完整状态响应
        private void onRequestBattleStateResponse(MainPack pack)
        {
            if (!checkResponse(pack, ActionCode.RequestBattleState)) return;
            broadcastEvents(pack);
        }

        // 检查响应是否成功
        private bool checkResponse(MainPack pack, ActionCode actionCode)
        {
            if (pack.ReturnCode == ReturnCode.Succeed) return true;

#if UNITY_EDITOR
            Debug.LogWarning($"[BattleNetwork] {actionCode} 失败: {pack.StrMsg}");
#endif
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
