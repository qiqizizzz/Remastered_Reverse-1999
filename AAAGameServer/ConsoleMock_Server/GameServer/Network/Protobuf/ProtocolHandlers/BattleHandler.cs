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
                case ActionCode.SubmitPvPteam:
                    handleSubmitPvPTeam(client, pack);
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
            response.StateSnapshot = battle.GetStateSnapshot(bp.PlayerId);
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

            int playerId = pack.BattlePack?.PlayerId ?? 1;
            var snapshot = battle.GetStateSnapshot(playerId);
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

            var room = client.Server.BattleManager.TryMatch();
            if (room == null)
            {
                var waitPack = new MainPack
                {
                    RequestCode = RequestCode.Battle,
                    ActionCode = ActionCode.JoinPvP,
                    ReturnCode = ReturnCode.Succeed,
                    StrMsg = "匹配中，等待对手...",
                    BattlePack = new BattlePack { IsMatchSuccess = false }
                };
                client.Send(waitPack.ToByteArray());
                return;
            }

            sendMatchSuccess(client.Server.GetClientByUsername(room.Player1), room, 1);
            sendMatchSuccess(client.Server.GetClientByUsername(room.Player2), room, 2);
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

        // 提交PvP阵容
        private void handleSubmitPvPTeam(Client client, MainPack pack)
        {
            if (!checkLogin(client, ActionCode.SubmitPvPteam)) return;

            var room = client.Server.BattleManager.GetPrepareRoom(client.UserName);
            if (room == null)
            {
                sendBattleError(client, ActionCode.SubmitPvPteam, "当前不在PvP准备房间中");
                return;
            }

            int submitterPlayerId = room.GetPlayerId(client.UserName);
            var battle = client.Server.BattleManager.SubmitPvpTeam(client.UserName, new List<int>(pack.BattlePack.HeroIds));
            if (battle == null)
            {
                var notifyPack = new BattlePack
                {
                    MatchId = room.MatchId,
                    PlayerId = submitterPlayerId,
                    IsTeamReady = false
                };
                
                sendBattleResponse(client.Server.GetClientByUsername(room.Player1), ActionCode.SubmitPvPteam, notifyPack);
                sendBattleResponse(client.Server.GetClientByUsername(room.Player2), ActionCode.SubmitPvPteam, notifyPack);
                return;
            }

            sendPvpBattleStart(client.Server.GetClientByUsername(room.Player1), battle, 1);
            sendPvpBattleStart(client.Server.GetClientByUsername(room.Player2), battle, 2);
        }

        #endregion
        
        #region 发送消息
        // 发送PvP匹配成功消息
        private void sendMatchSuccess(Client client, Network.Matchmaking.PvpPrepareRoom room, int playerId)
        {
            if (client == null) return;

            sendBattleResponse(client, ActionCode.JoinPvP, new BattlePack
            {
                MatchId = room.MatchId,
                PlayerId = playerId,
                IsMatchSuccess = true
            });
        }

        // 发送PvP战斗开始消息
        private void sendPvpBattleStart(Client client, BattleInstance battle, int playerId)
        {
            if (client == null) return;

            var battlePack = new BattlePack
            {
                PlayerId = playerId,
                IsTeamReady = true,
                StateSnapshot = battle.GetStateSnapshot(playerId)
            };
            battlePack.Events.AddRange(battle.CollectEvents());
            sendBattleResponse(client, ActionCode.SubmitPvPteam, battlePack);
        }

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
