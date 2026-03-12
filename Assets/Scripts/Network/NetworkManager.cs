/*
* ┌──────────────────────────────────┐
* │  描    述: 网络管理器(接收/发送消息等)                    
* │  类    名: NetworkManager.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Network.Clients;
using UnityEngine;

namespace Network
{
    public class NetworkManager
    {
        [Header("网络配置")] 
        public string serverIP = "127.0.0.1";
        public int serverPort = 8888;
        
        private Client _client;
        private string _log = "";

        public NetworkManager()
        {
            _client = new Client();
        }
        
        
    }
}