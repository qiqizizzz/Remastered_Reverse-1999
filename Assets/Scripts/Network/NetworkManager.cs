/*
* ┌──────────────────────────────────┐
* │  描    述: 网络管理器(接收/发送消息等)                    
* │  类    名: NetworkManager.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using GameProtocol;
using Network.Config;
using Network.Servers;
using UnityEngine;

namespace Network
{
    public class NetworkManager
    {
        //远端服务器代理
        private ServerProxy _server;

        public NetworkManager()
        {
            _server = new ServerProxy();
        }

        public void OnUpdate()
        {
            _server.Update();
        }

        // 全局连接入口
        public void Connect()
        {
            if (!_server.IsConnected)
            {
                Debug.Log($"正在尝试连接到 {NetworkConfig.DefaultIP}:{NetworkConfig.DefaultPort}...");
                _server.Connect(NetworkConfig.DefaultIP, NetworkConfig.DefaultPort);
            }
            else
            {
                Debug.Log("当前已连接到服务器，无需重复连接");
            }
        }

        //发送消息
        public void Send(MainPack pack)
        {
            _server.Send(pack);
        }
        
        //注册事件
        public void AddMessageHandler(ActionCode actionCode, System.Action<MainPack> handler)
        {
            _server.AddMessageHandler(actionCode, handler);
        }
        
        //移除事件
        public void RemoveMessageHandler(ActionCode actionCode, System.Action<MainPack> handler)
        {
            _server.RemoveMessageHandler(actionCode, handler);
        }
    }
}