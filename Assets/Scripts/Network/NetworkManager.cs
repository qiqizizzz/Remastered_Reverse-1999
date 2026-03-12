/*
* ┌──────────────────────────────────┐
* │  描    述: 网络管理器(接收/发送消息等)                    
* │  类    名: NetworkManager.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using GameProtocol;
using Google.Protobuf;
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
            
            BindEvents();//绑定事件
        }

        public void OnUpdate()
        {
            _client.Update();
        }

        //绑定事件
        private void BindEvents()
        {
            _client.OnConnected += onConnected;
            _client.OnDisconnected += onDisconnected;
            _client.OnMessageReceived += onHandleMessage;
        }

        //连接服务器
        public void Connect()
        {
            if (!_client.IsConnected)
            {
                Debug.Log($"正在尝试连接到{serverIP}:{serverPort}...");
                _client.Connect(serverIP,serverPort);
            }
            else
            {
                Debug.Log("当前已连接到服务器，无需重复连接");
            }
        }
        
        //发送消息
        public void Send(string msg)
        {
            if (_client.IsConnected)
            {
                _client.Send(msg);
            }
            else
            {
                Debug.LogError("发送失败,未连接到服务器");
            }
        }

        //发送Protobuf消息
        public void Send(MainPack pack)
        {
            byte[] data = pack.ToByteArray();//Protobuf序列化
            _client.Send(data);
        }

        //接收消息并解析
        private void onHandleMessage(byte[] data)
        {
            try
            {
                MainPack pack = MainPack.Parser.ParseFrom(data);//Protobuf反序列化
                Debug.Log($"解析到消息: Request={pack.RequestCode}, Action={pack.ActionCode}");
            }
            catch (Exception ex)
            {
                Debug.LogError("解析Protobuf失败" + ex.Message);
            }
        }
        
        private void onConnected()
        {
            Debug.Log("onConnected");
        }

        private void onDisconnected()
        {
            Debug.Log("onDisconnected");
        }
        
    }
}