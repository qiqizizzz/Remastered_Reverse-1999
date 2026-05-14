/*
* ┌──────────────────────────────────┐
* │  描    述: 登录与注册协议处理器
* │  类    名: LogonHandler.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using GameProtocol;
using GameServer.Common;
using Google.Protobuf;
using GameServer.Battle;
using Network.DataBase;

namespace Network
{
    internal class LogonHandler : IProtocolHandler
    {
        public void Handle(Client client, MainPack pack)
        {
            QLog.Info($"[收到请求]:{pack.RequestCode} - {pack.ActionCode}");

            if (pack.ActionCode == ActionCode.Logon)
                handleRegister(client, pack);
            else if (pack.ActionCode == ActionCode.Login)
                handleLogin(client, pack);
        }

        private void handleRegister(Client client, MainPack pack)
        {
            string username = pack.LoginPack.Username;
            string password = pack.LoginPack.Password;
            QLog.Info($"[尝试注册] 用户名:{username} 密码:{password}");

            bool isSuccess = DBManager.Register(username, password);

            MainPack resPack = new MainPack
            {
                RequestCode = RequestCode.User,
                ActionCode = ActionCode.Logon,
                ReturnCode = isSuccess ? ReturnCode.Succeed : ReturnCode.Failed,
                StrMsg = isSuccess ? "注册成功!" : "注册失败!用户名已存在"
            };

            QLog.Info(isSuccess ? $"{client} 注册成功" : $"{client} 注册失败 - 用户名已存在");
            client.Send(resPack.ToByteArray());
        }

        private void handleLogin(Client client, MainPack pack)
        {
            string username = pack.LoginPack.Username;
            string password = pack.LoginPack.Password;
            QLog.Info($"[{client}] 尝试登录: 账号={username}");

            int loginResult = DBManager.Login(username, password);

            MainPack resPack = new MainPack
            {
                RequestCode = RequestCode.User,
                ActionCode = ActionCode.Login
            };

            if (loginResult == 1)
            {
                client.SetUserName(username);
                client.Server.AddUserClient(username, client);

                resPack.ReturnCode = ReturnCode.Succeed;
                resPack.StrMsg = "登录成功！";
                QLog.Info($"[{client}] 登录成功");

                DBManager.UpdateLastLoginTime(username);
            }
            else if (loginResult == -2)
            {
                resPack.ReturnCode = ReturnCode.Failed;
                resPack.StrMsg = "登录失败，账号被封禁！";
                QLog.Info($"[{client}] 登录失败: 账号被封禁");
            }
            else
            {
                resPack.ReturnCode = ReturnCode.Failed;
                resPack.StrMsg = "登录失败，用户名或密码错误！";
                QLog.Info($"[{client}] 登录失败: 密码错误或用户不存在");
            }

            client.Send(resPack.ToByteArray());
        }
    }
}
