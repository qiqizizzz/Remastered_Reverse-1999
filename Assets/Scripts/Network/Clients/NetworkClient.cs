/*
* ┌──────────────────────────────────┐
* │  描    述: 客户端                      
* │  类    名: Client.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Network.Clients
{
    public class NetworkClient
    {
        private Socket _socket;
        private string _ip;
        private int _port;

        private ConcurrentQueue<byte[]> _receiveQueue = new ConcurrentQueue<byte[]>();//线程安全的消息队列
        public bool IsConnected => _socket != null && _socket.Connected;
        
        //回调事件
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<byte[]> OnMessageReceived; //已经处理的消息直接通知
        
        [Header("粘包处理相关")]
        //接收缓冲区
        private byte[] _receiveBuffer = new byte[1024 * 4];//4KB
        //消息组装相关
        private byte[] _messageBuffer = new byte[1024 * 1024];//1MB消息缓存
        private int _messageOffset = 0;//当前写入位置
        private int _messageLength = 0;//当前消息预期长度
        private bool _isHeaderReceived = false;//是否已接收消息头
        
        private const string SYS_CONNECTED = "[SYS]CONNECTED";
        private const string SYS_DISCONNECTED = "[SYS]DISCONNECTED";
        
        // 主线程调用：处理消息队列
        public void Update()
        {
            while (_receiveQueue.TryDequeue(out byte[] data))
            {
                if (data == null || data.Length == 0) continue;
                
                // 尝试将字节转回字符串
                string msgStr = bytesToString(data);
                
                if (msgStr == SYS_CONNECTED)
                    OnConnected?.Invoke();
                else if (msgStr == SYS_DISCONNECTED)
                    OnDisconnected?.Invoke();
                else
                    OnMessageReceived?.Invoke(data);//Protobuf字节流
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
                Debug.LogError($"连接失败: {e.Message}");
            }
        }
        
        //断开连接
        private void DisConnect()
        {
            if(_socket == null) return;
            
            try
            {
                _socket.Shutdown(SocketShutdown.Both);//Both:同时关闭发送和接收
                _socket.Close();
                _socket = null;
                Debug.Log("已断开连接");
            }
            catch (Exception ex)
            {
                Debug.LogError($"断开异常:{ex.Message}");
            }
        }
        
        private void OnConnectCallback(IAsyncResult ar)
        {
            try
            {
                _socket.EndConnect(ar);
                Debug.Log($"已连接到服务器 {_ip}:{_port}");
                
                // 通知主线程已连接
                _receiveQueue.Enqueue(stringToByte(SYS_CONNECTED));
                
                // 开始接收
                BeginReceive();
            }
            catch (Exception e)
            {
                Debug.LogError($"连接回调异常: {e.Message}");
                _receiveQueue.Enqueue(stringToByte(SYS_DISCONNECTED));
            }
        }
        
        //开始接收数据
        private void BeginReceive()
        {
            try
            {
                if (!IsConnected) return;

                _socket.BeginReceive(_receiveBuffer,0,_receiveBuffer.Length,SocketFlags.None, onReceiveCallback, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"开始接收失败:{ex.Message}");
                DisConnect();
            }
        }

        private void onReceiveCallback(IAsyncResult ar)
        {
            try
            {
                if (!IsConnected) return;

                int receiveLength = _socket.EndReceive(ar);

                if(receiveLength == 0)
                {
                    //客户端主动断开连接
                    Debug.Log(SYS_DISCONNECTED);
                    DisConnect();
                    return;
                }

                ProcessData(_receiveBuffer, receiveLength);//处理接收到的数据

                BeginReceive();//继续接收下一条数据
            }
            catch (Exception ex)
            {
                Debug.LogError($"接收异常:{ex.Message}");
                _receiveQueue.Enqueue(stringToByte(SYS_DISCONNECTED));
                DisConnect();
            }
        }
        
        //处理数据(解决粘包)
        private void ProcessData(byte[] data, int length)
        {
            int offset = 0;

            while(offset < length)
            {
                //如果还没读取消息头(4字节)
                if(!_isHeaderReceived)
                {
                    int need = 4 - _messageOffset;
                    int available = length - offset;
                    int toCopy = Math.Min(need, available);

                    Array.Copy(data, offset, _messageBuffer, _messageOffset, toCopy);//从接收缓冲区复制数据到消息缓冲区
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
                            Debug.LogError($"非法消息长度:{_messageLength}");
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
                    if(_messageOffset == _messageLength)
                    {
                        //拷贝字节数组存入队列
                        byte[] messagePayload = new byte[_messageLength];
                        Array.Copy(_messageBuffer, 0, messagePayload, 0, _messageLength);
                        _receiveQueue.Enqueue(messagePayload);
                        
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
                Debug.LogError($"发送失败: {ex.Message}");
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

                _socket.BeginSend(fullPacket, 0, fullPacket.Length, SocketFlags.None, onSendCallback, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"发送失败: {ex.Message}");
            }
        }
        
        //发送回调
        private void onSendCallback(IAsyncResult ar)
        {
            try { _socket.EndSend(ar); }
            catch (Exception ex) { Debug.LogError($"发送回调异常: {ex.Message}"); }
        }
        
        private byte[] stringToByte(string msg) => Encoding.UTF8.GetBytes(msg);
        private string bytesToString(byte[] bytes) => Encoding.UTF8.GetString(bytes);

    }
}