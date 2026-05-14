/*
* ┌──────────────────────────────────┐
* │  描    述: 私聊与聊天记录协议处理器
* │  类    名: ChatHandler.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using GameProtocol;
using GameServer.Common;
using Google.Protobuf;
using Network.DataBase;
using System;

namespace Network
{
    internal class ChatHandler : IProtocolHandler
    {
        public void Handle(Client client, MainPack pack)
        {
            if (pack.ActionCode == ActionCode.ChatPrivate)
                handleChatPrivate(client, pack);
            else if (pack.ActionCode == ActionCode.GetChatHistory)
                handleGetChatHistory(client, pack);
        }

        private void handleChatPrivate(Client client, MainPack pack)
        {
            if (string.IsNullOrEmpty(client.UserName))
            {
                MainPack errPack = new MainPack
                {
                    ActionCode = ActionCode.ChatPrivate,
                    ReturnCode = ReturnCode.Failed,
                    StrMsg = "请先登录!"
                };
                client.Send(errPack.ToByteArray());
                return;
            }

            string targetUser = pack.ChatPack.ToUser;
            string content = pack.ChatPack.Content;
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            DBManager.SaveChatMessage(client.UserName, targetUser, content);

            MainPack myResPack = new MainPack
            {
                ActionCode = ActionCode.ChatPrivate,
                ReturnCode = ReturnCode.Succeed,
                StrMsg = "发送成功"
            };
            client.Send(myResPack.ToByteArray());

            // 检查目标是否在线，转发消息
            Client targetClient = client.Server.GetClientByUsername(targetUser);
            if (targetClient != null)
            {
                ChatPack chatData = new ChatPack
                {
                    ToUser = targetUser,
                    FromUser = client.UserName,
                    Content = content,
                    Timestamp = timestamp
                };

                MainPack forwardPack = new MainPack
                {
                    ActionCode = ActionCode.ChatPrivate,
                    ReturnCode = ReturnCode.Succeed,
                    ChatPack = chatData
                };

                targetClient.Send(forwardPack.ToByteArray());
            }

            // 自己给自己发消息时刷新界面
            if (targetUser == client.UserName)
                handleGetChatHistory(client, pack);

            QLog.Info($"[私聊] {client.UserName} 发给 {targetUser}: {content}");
        }

        private void handleGetChatHistory(Client client, MainPack pack)
        {
            if (string.IsNullOrEmpty(client.UserName))
                return;

            string targetUser = pack.ChatHistoryPack.TargetUser;

            var msgs = DBManager.GetChatHistory(client.UserName, targetUser);

            MainPack resPack = new MainPack
            {
                ActionCode = ActionCode.GetChatHistory,
                ReturnCode = ReturnCode.Succeed,
                ChatHistoryPack = new ChatHistoryPack { TargetUser = targetUser }
            };

            foreach (var msg in msgs)
            {
                resPack.ChatHistoryPack.ChatList.Add(new ChatPack
                {
                    FromUser = msg.SenderName,
                    ToUser = msg.ReceiverName,
                    Content = msg.Content,
                    Timestamp = msg.SendTime,
                    ChatType = 0
                });
            }

            client.Send(resPack.ToByteArray());
        }
    }
}
