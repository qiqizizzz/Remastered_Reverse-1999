/*
* ┌──────────────────────────────────┐
* │  描    述: 服务器在客户端的代理                      
* │  类    名: ServerProxy.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Common;
using Common.Defines;
using GameProtocol;
using Google.Protobuf;
using UnityEngine;

namespace Network
{
    public class ServerProxy
    {
        private NetworkClient _client;
        private Dictionary<ActionCode, Action<MainPack>> _messageHandlers;//消息分发字典

        [Header("心跳相关参数")] 
        private float _pingInterval = 3f;//心跳间隔
        private float _timeoutLimit = 10f;//超时限制
        private float _lastPingTime = 0f;//上次发送心跳的时间
        private float _lastReceiveTime = 0f;//上次收到消息的时间
        
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
            
            //心跳检测
            if (IsConnected)
                CheckHeartbeat();
        }

        #region 服务端底层通信
        //心跳检测
        private void CheckHeartbeat()
        {
            float currentTime = Time.realtimeSinceStartup;

            //若超过心跳间隔，发送心跳检测
            if (currentTime - _lastPingTime > _pingInterval)
            {
                SendPing();
                _lastPingTime = currentTime;
            }

            //若超过超时限制，认为连接已断开
            if (currentTime - _lastReceiveTime > _timeoutLimit)
            {
                Debug.LogError("[ServerProxy] 心跳超时，连接已断开");
                onError("网络连接超时");
                
                //主动断开连接
                _client.DisConnect();
            }
        }

        private void SendPing()
        {
            MainPack pack = new MainPack();
            pack.RequestCode = RequestCode.User;
            pack.ActionCode = ActionCode.Heartbeat;
            Send(pack);
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
        
        #endregion

        #region 消息处理
        private void onConnected()
        {
            Debug.Log("连接成功");
            _lastPingTime = Time.realtimeSinceStartup;
            _lastReceiveTime = Time.realtimeSinceStartup;
        }
        
        private void onConnectFailed(string msg)
        {
            Debug.LogError($"[ServerProxy] 连接服务器失败，原因: {msg}");
        }

        private void onDisconnected()
        {
            GameApp.MessageCenter.PostEvent(EventDefines.NetWork_Disconnect);
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
                _lastReceiveTime = Time.realtimeSinceStartup;
                MainPack pack = MainPack.Parser.ParseFrom(data);//Protobuf反序列化
                
                //过滤心跳消息，不分发
                if(pack.ActionCode == ActionCode.Heartbeat) return;
                
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
        
        #endregion
    }
}