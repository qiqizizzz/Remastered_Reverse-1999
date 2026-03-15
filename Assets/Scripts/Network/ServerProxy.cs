/*
* ┌──────────────────────────────────┐
* │  描    述: 服务器在客户端的代理                      
* │  类    名: ServerProxy.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using GameProtocol;
using Google.Protobuf;
using UnityEngine;

namespace Network
{
    public class ServerProxy
    {
        private NetworkClient _client;
        
        //消息分发字典
        private Dictionary<ActionCode, Action<MainPack>> _messageHandlers;
        
        public bool IsConnected => _client != null && _client.IsConnected;

        public ServerProxy()
        {
            _client = new NetworkClient();
            _messageHandlers = new Dictionary<ActionCode, Action<MainPack>>();

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
            _client.OnConnectFailed += onConnectFailed;
            _client.OnDisconnected += onDisconnected;
            _client.OnError += onError;
            _client.OnMessageReceived += onHandleMessage;
        }
        
        //连接服务器
        public void Connect(string ip, int port)
        {
            if (!_client.IsConnected)
            {
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
            Debug.Log("连接成功");
        }
        
        private void onConnectFailed(string msg)
        {
            Debug.LogError($"[ServerProxy] 连接服务器失败，原因: {msg}");
        }

        private void onDisconnected()
        {
            Debug.Log("断开连接");
        }

        private void onError(string msg)
        {
            Debug.LogError($"[ServerProxy] 网络发生异常掉线，原因: {msg}");
        }
        
        //接收消息并解析
        private void onHandleMessage(byte[] data)
        {
            try
            {
                MainPack pack = MainPack.Parser.ParseFrom(data);//Protobuf反序列化
                Debug.Log($"[ServerProxy] 解析到消息: Request={pack.RequestCode}, Action={pack.ActionCode}");
                
                //分发消息
                if (_messageHandlers.TryGetValue(pack.ActionCode, out var handler))
                {
                    handler?.Invoke(pack);
                }
                else
                {
                    Debug.Log($"[ServerProxy] 未找到消息处理器: ActionCode={pack.ActionCode}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("解析Protobuf失败" + ex.Message);
            }
        }
        
        //添加消息处理器
        public void AddMessageHandler(ActionCode actionCode, Action<MainPack> handler)
        {
            if (!_messageHandlers.ContainsKey(actionCode))
            {
                _messageHandlers.Add(actionCode, handler);
            }
            else
            {
                _messageHandlers[actionCode] += handler;
            }
        }
        
        //移除消息处理器
        public void RemoveMessageHandler(ActionCode actionCode, Action<MainPack> handler)
        {
            if (_messageHandlers.ContainsKey(actionCode))
                _messageHandlers[actionCode] -= handler;
        }
    }
}