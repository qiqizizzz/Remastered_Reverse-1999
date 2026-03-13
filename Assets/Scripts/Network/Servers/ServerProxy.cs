/*
* ┌──────────────────────────────────┐
* │  描    述: 服务器在客户端的代理                      
* │  类    名: ServerProxy.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using GameProtocol;
using Google.Protobuf;
using Network.Clients;
using UnityEngine;

namespace Network.Servers
{
    public class ServerProxy
    {
        private NetworkClient _client;
        
        public bool IsConnected => _client != null && _client.IsConnected;

        public ServerProxy()
        {
            _client = new NetworkClient();

            BindEvents();
        }
        
        public void Update()
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
        public void Connect(string ip, int port)
        {
            if (!_client.IsConnected)
            {
                Debug.Log($"正在尝试连接到{ip}:{port}...");
                _client.Connect(ip, port);
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
            if (_client.IsConnected)
            {
                byte[] data = pack.ToByteArray();
                _client.Send(data);
            }
            else
            {
                Debug.LogError("[ServerProxy] 发送失败，未连接到服务器");
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
        
        //接收消息并解析
        private void onHandleMessage(byte[] data)
        {
            try
            {
                MainPack pack = MainPack.Parser.ParseFrom(data);//Protobuf反序列化
                Debug.Log($"[ServerProxy] 解析到消息: Request={pack.RequestCode}, Action={pack.ActionCode}");
            }
            catch (Exception ex)
            {
                Debug.LogError("解析Protobuf失败" + ex.Message);
            }
        }
    }
}