/*
* ┌──────────────────────────────────┐
* │  描    述: 协议路由器，根据 ActionCode 将消息分发到对应 Handler
* │  类    名: ProtocolRouter.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using GameProtocol;
using System.Collections.Generic;

namespace Network
{
    internal static class ProtocolRouter
    {
        private static readonly Dictionary<ActionCode, IProtocolHandler> _handlers = new();

        public static void Register(ActionCode code, IProtocolHandler handler)
        {
            _handlers[code] = handler;
        }

        public static bool Route(Client client, MainPack pack)
        {
            if (_handlers.TryGetValue(pack.ActionCode, out var handler))
            {
                handler.Handle(client, pack);
                return true;
            }
            return false;
        }
    }
}
