/*
* ┌──────────────────────────────────┐
* │  描    述: 战斗协议处理器（进入/出牌/移牌/结束/撤销/重连）
* │  类    名: BattleHandler.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using GameProtocol;
using GameServer.Battle;
using Google.Protobuf;
using System.Collections.Generic;

namespace Network
{
    internal class BattleHandler : IProtocolHandler
    {
        public void Handle(Client client, MainPack pack)
        {
            switch (pack.ActionCode)
            {
                case ActionCode.EnterPve:
                    handleEnterPve(client, pack);
                    break;
                case ActionCode.PlayCard:
                    handlePlayCard(client, pack);
                    break;
                case ActionCode.EndTurn:
                case ActionCode.CommitRound:
                    handleEndTurn(client, pack);
                    break;
                case ActionCode.MoveCard:
                    handleMoveCard(client, pack);
                    break;
                case ActionCode.UnDoAction:
                    handleUndoAction(client, pack);
                    break;
                case ActionCode.RequestBattleState:
                    handleRequestBattleState(client, pack);
                    break;
                case ActionCode.JoinPvP:
                    handleJoinPvP(client, pack);
                    break;
                case ActionCode.LeavePvP:
                    handleLeavePvP(client, pack);
                    break;
            }
        }

        private bool checkLogin(Client client, ActionCode actionCode)
        {
            if (!string.IsNullOrEmpty(client.UserName))
                return true;

            sendBattleError(client, actionCode, "请先登录");
            return false;
        }

        #region 处理消息
        private void handleEnterPve(Client client, MainPack pack)
        {
            if (!checkLogin(client, ActionCode.EnterPve)) return;

            int levelId = pack.BattlePack?.LevelId ?? 0;
            var battle = client.Server.BattleManager.CreateBattle(client.UserName, levelId);
            if (battle == null)
            {
                sendBattleError(client, ActionCode.EnterPve, "关卡不存在或创建失败");
                return;
            }

            var events = battle.CollectEvents();
            var response = new BattlePack();
            response.Events.AddRange(events);
            response.StateSnapshot = battle.GetStateSnapshot();
            sendBattleResponse(client, ActionCode.EnterPve, response);
        }

        private void handlePlayCard(Client client, MainPack pack)
        {
            if (!checkLogin(client, ActionCode.PlayCard)) return;

            var battle = client.Server.BattleManager.GetBattle(client.UserName);
            if (battle == null)
            {
                sendBattleError(client, ActionCode.PlayCard, "当前不在战斗中");
                return;
            }

            var bp = pack.BattlePack;
            bool success = battle.PlayCard(bp.PlayerId, bp.CardInstanceId, bp.TargetEntityId, bp.SourceSlotIndex);
            if (!success)
            {
                sendBattleError(client, ActionCode.PlayCard, "出牌失败");
                return;
            }

            var events = battle.CollectEvents();
            sendBattleResponse(client, ActionCode.PlayCard, events);
        }

        private void handleEndTurn(Client client, MainPack pack)
        {
            if (!checkLogin(client, ActionCode.EndTurn)) return;

            var battle = client.Server.BattleManager.GetBattle(client.UserName);
            if (battle == null)
            {
                sendBattleError(client, ActionCode.EndTurn, "当前不在战斗中");
                return;
            }

            var bp = pack.BattlePack;
            battle.EndTurn(bp.PlayerId);
            var events = battle.CollectEvents();
            sendBattleResponse(client, ActionCode.EndTurn, events);

            if (battle.Result != BattleResult.None)
                client.Server.BattleManager.RemoveBattle(client.UserName);
        }

        private void handleMoveCard(Client client, MainPack pack)
        {
            if (!checkLogin(client, ActionCode.MoveCard)) return;

            var battle = client.Server.BattleManager.GetBattle(client.UserName);
            if (battle == null)
            {
                sendBattleError(client, ActionCode.MoveCard, "当前不在战斗中");
                return;
            }

            var bp = pack.BattlePack;
            bool success = battle.MoveCard(bp.PlayerId, bp.SourceSlotIndex, bp.TargetSlotIndex);
            if (!success)
            {
                sendBattleError(client, ActionCode.MoveCard, "移动卡牌失败");
                return;
            }

            var events = battle.CollectEvents();
            sendBattleResponse(client, ActionCode.MoveCard, events);
        }

        private void handleUndoAction(Client client, MainPack pack)
        {
            if (!checkLogin(client, ActionCode.UnDoAction)) return;

            var battle = client.Server.BattleManager.GetBattle(client.UserName);
            if (battle == null)
            {
                sendBattleError(client, ActionCode.UnDoAction, "当前不在战斗中");
                return;
            }

            var bp = pack.BattlePack;
            bool success = battle.Undo(bp.PlayerId);
            if (!success)
            {
                sendBattleError(client, ActionCode.UnDoAction, "撤销失败");
                return;
            }

            var events = battle.CollectEvents();
            var response = new BattlePack();
            response.Events.AddRange(events);
            response.StateSnapshot = battle.GetStateSnapshot();
            sendBattleResponse(client, ActionCode.UnDoAction, response);
        }

        private void handleRequestBattleState(Client client, MainPack pack)
        {
            if (!checkLogin(client, ActionCode.RequestBattleState)) return;

            var battle = client.Server.BattleManager.GetBattle(client.UserName);
            if (battle == null)
            {
                sendBattleError(client, ActionCode.RequestBattleState, "当前不在战斗中");
                return;
            }

            var snapshot = battle.GetStateSnapshot();
            var events = battle.CollectEvents();

            var resPack = new MainPack
            {
                RequestCode = RequestCode.Battle,
                ActionCode = ActionCode.RequestBattleState,
                ReturnCode = ReturnCode.Succeed,
                BattlePack = new BattlePack { StateSnapshot = snapshot }
            };
            resPack.BattlePack.Events.AddRange(events);
            client.Send(resPack.ToByteArray());
        }

        private void handleJoinPvP(Client client, MainPack pack)
        {
            if (!checkLogin(client, ActionCode.JoinPvP)) return;

            client.Server.BattleManager.JoinQueue(client.UserName);

            var matchResult = client.Server.BattleManager.TryMatch();
            if (matchResult == null)
            {
                var waitPack = new MainPack
                {
                    RequestCode = RequestCode.Battle,
                    ActionCode = ActionCode.JoinPvP,
                    ReturnCode = ReturnCode.Succeed,
                    StrMsg = "匹配中，等待对手..."
                };
                client.Send(waitPack.ToByteArray());
                return;
            }

            var (p1, p2, battle) = matchResult.Value;
            var events = battle.CollectEvents();

            var clientP1 = client.Server.GetClientByUsername(p1);
            if (clientP1 != null)
            {
                var res1 = new BattlePack { PlayerId = 1 };
                res1.Events.AddRange(events);
                res1.StateSnapshot = battle.GetStateSnapshot();
                sendBattleResponse(clientP1, ActionCode.JoinPvP, res1);
            }

            var clientP2 = client.Server.GetClientByUsername(p2);
            if (clientP2 != null)
            {
                var res2 = new BattlePack { PlayerId = 2 };
                res2.Events.AddRange(events);
                res2.StateSnapshot = battle.GetStateSnapshot();
                sendBattleResponse(clientP2, ActionCode.JoinPvP, res2);
            }
        }

        private void handleLeavePvP(Client client, MainPack pack)
        {
            if (!checkLogin(client, ActionCode.LeavePvP)) return;

            client.Server.BattleManager.LeaveQueue(client.UserName);

            var resPack = new MainPack
            {
                RequestCode = RequestCode.Battle,
                ActionCode = ActionCode.LeavePvP,
                ReturnCode = ReturnCode.Succeed,
                StrMsg = "已离开匹配队列"
            };
            client.Send(resPack.ToByteArray());
        }

        #endregion
        
        #region 发送消息
        private void sendBattleError(Client client, ActionCode actionCode, string msg)
        {
            var resPack = new MainPack
            {
                RequestCode = RequestCode.Battle,
                ActionCode = actionCode,
                ReturnCode = ReturnCode.Failed,
                StrMsg = msg
            };
            client.Send(resPack.ToByteArray());
        }

        private void sendBattleResponse(Client client, ActionCode actionCode, List<BattleEvent> events)
        {
            var battlePack = new BattlePack();
            battlePack.Events.AddRange(events);
            sendBattleResponse(client, actionCode, battlePack);
        }

        private void sendBattleResponse(Client client, ActionCode actionCode, BattlePack battlePack)
        {
            var resPack = new MainPack
            {
                RequestCode = RequestCode.Battle,
                ActionCode = actionCode,
                ReturnCode = ReturnCode.Succeed,
                BattlePack = battlePack ?? new BattlePack()
            };
            client.Send(resPack.ToByteArray());
        }
        #endregion
    }
}
