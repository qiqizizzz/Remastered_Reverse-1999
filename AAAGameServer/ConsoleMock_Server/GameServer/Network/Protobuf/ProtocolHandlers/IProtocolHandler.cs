/*
* ┌──────────────────────────────────┐
* │  描    述: 协议处理器接口，每个协议包对应一个处理器实现
* │  类    名: IProtocolHandler.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using GameProtocol;

namespace Network
{
    internal interface IProtocolHandler
    {
        void Handle(Client client, MainPack pack);
    }
}
