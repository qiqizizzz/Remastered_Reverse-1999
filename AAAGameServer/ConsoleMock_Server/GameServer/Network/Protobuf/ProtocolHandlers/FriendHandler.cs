/*
* ┌──────────────────────────────────┐
* │  描    述: 好友操作协议处理器（获取列表/添加/删除/搜索）
* │  类    名: FriendHandler.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using GameProtocol;
using GameServer.Common;
using Google.Protobuf;
using Network.DataBase;
using Network.DataBase.Entity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Network
{
    internal class FriendHandler : IProtocolHandler
    {
        public void Handle(Client client, MainPack pack)
        {
            if (string.IsNullOrEmpty(client.UserName))
            {
                MainPack errPack = new MainPack
                {
                    ActionCode = pack.ActionCode,
                    ReturnCode = ReturnCode.Failed,
                    StrMsg = "请先登录!"
                };
                client.Send(errPack.ToByteArray());
                return;
            }

            switch (pack.FriendPack.OpType)
            {
                case FriendOpType.FriendOpNone:
                    break;
                case FriendOpType.GetList:
                    handleGetFriendList(client, pack);
                    break;
                case FriendOpType.AddFriend:
                    handleAddFriend(client, pack);
                    break;
                case FriendOpType.RemoveFriend:
                    handleRemoveFriend(client, pack);
                    break;
                case FriendOpType.SearchUser:
                    handleSearchUser(client, pack);
                    break;
            }
        }

        private void handleGetFriendList(Client client, MainPack pack)
        {
            List<Friend> friends = DBManager.GetFriends(client.UserName);

            MainPack resPack = new MainPack
            {
                RequestCode = RequestCode.Friend,
                ActionCode = ActionCode.FriendOperation,
                ReturnCode = ReturnCode.Succeed,
                FriendPack = new FriendPack { OpType = FriendOpType.GetList }
            };

            foreach (var item in friends)
            {
                bool isOnline = client.Server.GetClientByUsername(item.FriendUsername) != null;

                QLog.Info($"[Server] 好友: {item}, 找到客户端: {client}, 判定在线: {isOnline}");
                resPack.FriendPack.FriendList.Add(new FriendInfo
                {
                    Username = item.FriendUsername,
                    IsOnline = isOnline
                });
            }

            client.Send(resPack.ToByteArray());
            QLog.Info($"[{client}] 获取好友列表,好友数量: {friends.Count}");
        }

        private void handleAddFriend(Client client, MainPack pack)
        {
            bool success = DBManager.AddFriend(client.UserName, pack.FriendPack.TargetUser);

            MainPack resPack = new MainPack
            {
                RequestCode = RequestCode.Friend,
                ActionCode = ActionCode.FriendOperation,
                FriendPack = new FriendPack
                {
                    OpType = FriendOpType.AddFriend,
                    TargetUser = pack.FriendPack.TargetUser
                }
            };

            if (success)
            {
                resPack.ReturnCode = ReturnCode.Succeed;
                QLog.Info($"[{client}] 添加好友成功: {pack.FriendPack.TargetUser}");
            }
            else
            {
                resPack.ReturnCode = ReturnCode.Failed;
                QLog.Info($"[{client}] 添加好友失败: {pack.FriendPack.TargetUser}");
            }

            client.Send(resPack.ToByteArray());
        }

        private void handleRemoveFriend(Client client, MainPack pack)
        {
            bool success = DBManager.RemoveFriend(client.UserName, pack.FriendPack.TargetUser);

            MainPack resPack = new MainPack
            {
                RequestCode = RequestCode.Friend,
                ActionCode = ActionCode.FriendOperation,
                FriendPack = new FriendPack
                {
                    OpType = FriendOpType.RemoveFriend,
                    TargetUser = pack.FriendPack.TargetUser
                }
            };

            if (success)
            {
                resPack.ReturnCode = ReturnCode.Succeed;
                QLog.Info($"[{client}] 删除好友成功: {pack.FriendPack.TargetUser}");
            }
            else
            {
                resPack.ReturnCode = ReturnCode.Failed;
                QLog.Info($"[{client}] 删除好友失败: {pack.FriendPack.TargetUser}");
            }

            client.Send(resPack.ToByteArray());
        }

        private void handleSearchUser(Client client, MainPack pack)
        {
            List<string> userList = DBManager.SearchUser(pack.FriendPack.TargetUser);
            List<string> userFriend = DBManager.GetFriendsName(client.UserName);

            List<string> filteredList = userList.Except(userFriend).ToList();

            MainPack resPack = new MainPack
            {
                RequestCode = RequestCode.Friend,
                ActionCode = ActionCode.FriendOperation,
                ReturnCode = ReturnCode.Succeed,
                FriendPack = new FriendPack { OpType = FriendOpType.SearchUser }
            };

            if (filteredList.Count == 0)
            {
                QLog.Info($"[{client}] 搜索无结果: {pack.FriendPack.TargetUser}");
            }

            if (filteredList.Count > 0)
            {
                resPack.ReturnCode = ReturnCode.Succeed;
                QLog.Info($"[Server] 发送搜索数据: {string.Join(", ", filteredList)}");

                for (int i = 0; i < filteredList.Count; i++)
                {
                    resPack.FriendPack.FriendList.Add(new FriendInfo
                    {
                        Username = filteredList[i],
                        IsOnline = client.Server.GetClientByUsername(filteredList[i]) != null
                    });
                }

                QLog.Info($"[Server] FriendList.Count = {resPack.FriendPack.FriendList.Count}");
            }

            client.Send(resPack.ToByteArray());
        }
    }
}
