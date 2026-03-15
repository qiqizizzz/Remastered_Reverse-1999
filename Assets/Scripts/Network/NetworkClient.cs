/*
* ┌──────────────────────────────────┐
* │  描    述: 客户端                      
* │  类    名: Client.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace Network
{
    //网络事件类型
    public enum NetEventType
    {
        Connected,
        ConnectFailed,
        Disconnected,
        Error, 
        Data
    }

    //网络事件结构
    public struct NetEvent
    {
        public NetEventType Type;
        public byte[] Data;
        public string Message; //可选的文本消息
    }

    public class NetworkClient
    {
        private Socket _socket;
        private string _ip;
        private int _port;

        //回调事件
        public event Action OnConnected;
        public event Action<string> OnConnectFailed; //连接失败(IP错误、服务器没开等)
        public event Action OnDisconnected; //断开连接
        public event Action<string> OnError; //发生异常(网络错误,Socket异常等)
        public event Action<byte[]> OnMessageReceived; //已经处理的消息直接通知

        [Header("接收队列相关")]
        private ConcurrentQueue<NetEvent> _receiveQueue = new ConcurrentQueue<NetEvent>(); //线程安全的消息队列

        public bool IsConnected => _socket != null && _socket.Connected;
        [Header("发送队列相关")] private Queue<byte[]> _sendQueue = new Queue<byte[]>();
        private bool _isSending = false; //是否正在发送中

        [Header("粘包处理相关")]
        //接收缓冲区
        private byte[] _receiveBuffer = new byte[1024 * 4]; //4KB

        //消息组装相关
        private byte[] _messageBuffer = new byte[1024 * 1024]; //1MB消息缓存
        private int _messageOffset = 0; //当前写入位置
        private int _messageLength = 0; //当前消息预期长度
        private bool _isHeaderReceived = false; //是否已接收消息头

        // 主线程调用：处理消息队列
        public void Update()
        {
            while (_receiveQueue.TryDequeue(out NetEvent netEvent))
            {
                switch (netEvent.Type)
                {
                    case NetEventType.Connected:
                        OnConnected?.Invoke();
                        break;
                    case NetEventType.ConnectFailed:
                        OnConnectFailed?.Invoke(netEvent.Message);
                        break;
                    case NetEventType.Disconnected:
                        OnDisconnected?.Invoke();
                        break;
                    case NetEventType.Error:
                        OnError?.Invoke(netEvent.Message);
                        break;
                    case NetEventType.Data:
                        if (netEvent.Data != null && netEvent.Data.Length > 0)
                            OnMessageReceived?.Invoke(netEvent.Data); //Protobuf字节流
                        break;
                }
            }
        }

        //连接
        public void Connect(string ip, int port)
        {
            _ip = ip;
            _port = port;

            try
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socket.BeginConnect(ip, port, OnConnectCallback, null);
            }
            catch (Exception e)
            {
                _receiveQueue.Enqueue(setNetEvent(NetEventType.ConnectFailed, null, $"连接失败: {e.Message}"));
            }
        }

        //断开连接
        public void DisConnect()
        {
            if (_socket == null) return;

            try
            {
                _socket.Shutdown(SocketShutdown.Both); //Both:同时关闭发送和接收
                _socket.Close();
                _socket = null;
                _receiveQueue.Enqueue(setNetEvent(NetEventType.Disconnected));
            }
            catch (Exception ex)
            {
                _receiveQueue.Enqueue(setNetEvent(NetEventType.Error, null, $"断开异常:{ex.Message}"));
            }
        }

        private void OnConnectCallback(IAsyncResult ar)
        {
            try
            {
                _socket.EndConnect(ar);

                // 通知主线程已连接
                _receiveQueue.Enqueue(setNetEvent(NetEventType.Connected));

                // 开始接收
                BeginReceive();
            }
            catch (Exception e)
            {
                _receiveQueue.Enqueue(setNetEvent(NetEventType.ConnectFailed, null, e.Message));
            }
        }

        //开始接收数据
        private void BeginReceive()
        {
            try
            {
                if (!IsConnected) return;

                _socket.BeginReceive(_receiveBuffer, 0, _receiveBuffer.Length, SocketFlags.None, onReceiveCallback,
                    null);
            }
            catch (Exception ex)
            {
                _receiveQueue.Enqueue(setNetEvent(NetEventType.Error, null, $"接收数据异常: {ex.Message}"));
                DisConnect();
            }
        }

        private void onReceiveCallback(IAsyncResult ar)
        {
            try
            {
                if (!IsConnected) return;

                int receiveLength = _socket.EndReceive(ar);

                if (receiveLength == 0)
                {
                    //服务器主动断开连接
                    _receiveQueue.Enqueue(setNetEvent(NetEventType.Disconnected));
                    DisConnect();
                    return;
                }

                ProcessData(_receiveBuffer, receiveLength); //处理接收到的数据

                BeginReceive(); //继续接收下一条数据
            }
            catch (Exception ex)
            {
                _receiveQueue.Enqueue(setNetEvent(NetEventType.Error, null, $"接收数据回调异常:{ex.Message}"));
                DisConnect();
            }
        }

        //处理数据(解决粘包)
        private void ProcessData(byte[] data, int length)
        {
            int offset = 0;

            while (offset < length)
            {
                //如果还没读取消息头(4字节)
                if (!_isHeaderReceived)
                {
                    int need = 4 - _messageOffset;
                    int available = length - offset;
                    int toCopy = Math.Min(need, available);

                    Array.Copy(data, offset, _messageBuffer, _messageOffset, toCopy); //从接收缓冲区复制数据到消息缓冲区
                    _messageOffset += toCopy;
                    offset += toCopy;


                    //如果凑够了4字节,解析消息长度
                    if (_messageOffset == 4)
                    {
                        _messageLength = BitConverter.ToInt32(_messageBuffer, 0);
                        _messageOffset = 0;
                        _isHeaderReceived = true;

                        //检查消息长度合法性
                        if (_messageLength <= 0 || _messageLength > _messageBuffer.Length)
                        {
                            _receiveQueue.Enqueue(setNetEvent(NetEventType.Error, null, $"非法消息长度:{_messageLength}"));
                            DisConnect();
                            return;
                        }
                    }
                }
                else
                {
                    //读取消息体
                    int need = _messageLength - _messageOffset;
                    int available = length - offset;
                    int toCopy = Math.Min(need, available);

                    Array.Copy(data, offset, _messageBuffer, _messageOffset, toCopy);
                    _messageOffset += toCopy;
                    offset += toCopy;

                    //消息接收完整
                    if (_messageOffset == _messageLength)
                    {
                        //拷贝字节数组存入队列
                        byte[] messagePayload = new byte[_messageLength];
                        Array.Copy(_messageBuffer, 0, messagePayload, 0, _messageLength);
                        _receiveQueue.Enqueue(setNetEvent(NetEventType.Data, messagePayload));

                        //重置状态,准备接收下一条消息
                        _messageOffset = 0;
                        _isHeaderReceived = false;
                    }
                }


            }
        }

        //发送数据
        public void Send(string message)
        {
            try
            {
                if (!IsConnected) return;

                //添加长度头
                byte[] body = Encoding.UTF8.GetBytes(message);
                byte[] header = BitConverter.GetBytes(body.Length);
                byte[] data = new byte[header.Length + body.Length];

                //组合消息 - 先头后体到data中
                Array.Copy(header, 0, data, 0, 4);
                Array.Copy(body, 0, data, 4, body.Length);

                //异步发送
                _socket.BeginSend(data, 0, data.Length, SocketFlags.None, onSendCallback, null);
            }
            catch (Exception ex)
            {
                _receiveQueue.Enqueue(setNetEvent(NetEventType.Error, null, $"向服务器发送消息失败:{ex.Message}"));
            }
        }

        public void Send(byte[] data)
        {
            try
            {
                if (!IsConnected) return;

                byte[] header = BitConverter.GetBytes(data.Length);
                byte[] fullPacket = new byte[header.Length + data.Length];

                Array.Copy(header, 0, fullPacket, 0, 4);
                Array.Copy(data, 0, fullPacket, 4, data.Length);

                //入队发送,加锁保证多线程/并发安全
                lock (_sendQueue)
                {
                    _sendQueue.Enqueue(fullPacket);

                    if (!_isSending)
                    {
                        _isSending = true;
                        SendNext();
                    }
                }
            }
            catch (Exception ex)
            {
                _receiveQueue.Enqueue(setNetEvent(NetEventType.Error, null, $"发送压栈失败:{ex.Message}"));
            }
        }

        private void SendNext()
        {
            lock (_sendQueue)
            {
                if (_sendQueue.Count == 0)
                {
                    _isSending = false;
                    return;
                }
                
                byte[] packet = _sendQueue.Dequeue();//出队一个包进行发送

                try
                {
                    _socket.BeginSend(packet, 0, packet.Length, SocketFlags.None, onSendCallback, null);
                }
                catch (Exception e)
                {
                    _receiveQueue.Enqueue(setNetEvent(NetEventType.Error, null, $"发送数据异常:{e.Message}"));
                    _isSending = false;
                    DisConnect();
                }
            }
        }

        //发送回调
        private void onSendCallback(IAsyncResult ar)
        {
            try
            {
                if (_socket != null)
                    _socket.EndSend(ar);

                SendNext();
            }
            catch (Exception ex)
            {
                _receiveQueue.Enqueue(setNetEvent(NetEventType.Error, null, $"发送数据回调异常:{ex.Message}"));
                _isSending = false;
                DisConnect();
            }
        }
        
        //字符串转字节数组
        private NetEvent setNetEvent(NetEventType type, byte[] data = null, string message = null)
        {
            return new NetEvent { Type = type, Data = data, Message = message };
        }
    }
}