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
using Network.Config;
using UnityEngine;

namespace Network
{
    public class NetworkManager
    {
        private NetworkClient _networkClient;
        private NetworkConfig _config;

        public NetworkManager()
        {
            _networkClient = new NetworkClient();
            
            BindEvents();//绑定事件
        }

        public void OnUpdate()
        {
            _networkClient.Update();
        }

        //绑定事件
        private void BindEvents()
        {
            _networkClient.OnConnected += onConnected;
            _networkClient.OnDisconnected += onDisconnected;
            _networkClient.OnMessageReceived += onHandleMessage;
        }

        //连接服务器
        public void Connect()
        {
            if (!_networkClient.IsConnected)
            {
                //Debug.Log($"正在尝试连接到{serverIP}:{serverPort}...");
                //_networkClient.Connect(serverIP,serverPort);
            }
            else
            {
                Debug.Log("当前已连接到服务器，无需重复连接");
            }
        }
        
        //发送消息
        public void Send(string msg)
        {
            if (_networkClient.IsConnected)
            {
                _networkClient.Send(msg);
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
            _networkClient.Send(data);
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