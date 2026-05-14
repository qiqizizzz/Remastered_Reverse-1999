/*
* ┌──────────────────────────────────┐
* │  描    述: 心跳协议处理器，保持客户端与服务器长连接
* │  类    名: HeartbeatHandler.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using GameProtocol;
using Google.Protobuf;
namespace Network
{
    internal class HeartbeatHandler : IProtocolHandler
    {
        public void Handle(Client client, MainPack pack)
        {
            MainPack resPack = new MainPack { ActionCode = ActionCode.Heartbeat };
            client.Send(resPack.ToByteArray());
        }
    }
}
