using GameProtocol;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace GameServer
{
    //客户端
    internal class Client
    {
        private Socket _socket;
        private string _clientId;

        //接收缓冲区
        private byte[] _receiveBuffer = new byte[1024 * 4];//4KB

        //消息组装相关
        private byte[] _messageBuffer = new byte[1024 * 1024];//1MB消息缓存
        private int _messageOffset = 0;//当前写入位置
        private int _messageLength = 0;//当前消息预期长度

        private bool _isHeaderReceived = false;//是否已接收消息头


        public Client(Socket socket, string clientId)
        {
            this._socket = socket;
            this._clientId = clientId;

            BeginReceive();
        }

        //开始接收数据
        private void BeginReceive()
        {
            try
            {
                if (_socket == null || !_socket.Connected) return;

                _socket.BeginReceive(_receiveBuffer,0,_receiveBuffer.Length,SocketFlags.None, onReceiveCallback, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{_clientId}] 开始接收失败:{ex.Message}");
                DisConnect();
            }
        }

        private void onReceiveCallback(IAsyncResult ar)
        {
            try
            {
                if (_socket == null || !_socket.Connected) return;

                int receiveLength = _socket.EndReceive(ar);

                if(receiveLength == 0)
                {
                    //客户端主动断开连接
                    Console.WriteLine($"[{_clientId}] 客户端已断开连接");
                    DisConnect();
                    return;
                }

                ProcessData(_receiveBuffer, receiveLength);//处理接收到的数据

                BeginReceive();//继续接收下一条数据
            }
            catch (SocketException se)
            {
                Console.WriteLine($"[{_clientId}] Socket异常:{se.Message}");
                DisConnect();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{_clientId}] 接收异常:{ex.Message}");
                DisConnect();
            }
        }

        //断开连接
        private void DisConnect()
        {
            try
            {
                if(_socket != null)
                {
                    _socket.Shutdown(SocketShutdown.Both);//Both:同时关闭发送和接收
                    _socket.Close();
                    _socket = null;
                }
                Console.WriteLine($"[{_clientId}] 连接已清理");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{_clientId}] 断开异常:{ex.Message}");
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
                            Console.WriteLine($"[{_clientId}] 非法消息长度:{_messageLength}");
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
                        byte[] message = new byte[_messageLength];
                        Array.Copy(_messageBuffer, 0, message, 0, _messageLength);

                        ThreadPool.QueueUserWorkItem(_ => OnMessageReceived(message));//避免阻塞接收,在线程池中处理

                        //重置状态,准备接收下一条消息
                        _messageOffset = 0;
                        _isHeaderReceived = false;
                    }
                }

                
            }
        }

        //收到完整消息时的处理
        private void OnMessageReceived(byte[] message)
        {
            try
            {
                HandleGameMessage(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{_clientId}] 处理消息异常: {ex.Message}");
            }
        }

        //根据游戏协议处理消息
        private void HandleGameMessage(byte[] message)
        {
            //解析客户端发来的包
            MainPack pack = MainPack.Parser.ParseFrom(message);
            Console.Write($"收到客户端请求:{pack.RequestCode} - {pack.ActionCode}");

            //根据Protobuf协议分发
            
            //登陆
            if (pack.ActionCode == ActionCode.Login)
            {
                Console.WriteLine($"用户登录尝试:{pack.LoginPack.Username}");
                Console.WriteLine($"密码是: {pack.LoginPack.Password}");

                //回包给客户端
                MainPack resPack = new MainPack();
                resPack.ReturnCode = ReturnCode.Succeed;
                Send(resPack.ToByteArray());
            }
        }

        //发送数据给客户端
        public void Send(byte[] data)
        {
            try
            {
                if (_socket == null || !_socket.Connected) return;

                byte[] header = BitConverter.GetBytes(data.Length);
                byte[] fullPacket = new byte[header.Length + data.Length];

                Array.Copy(header, 0, fullPacket, 0, 4);
                Array.Copy(data, 0, fullPacket, 4, data.Length);

                _socket.BeginSend(fullPacket, 0, fullPacket.Length, SocketFlags.None, onSendCallback, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{_clientId}] 发送字节失败: {ex.Message}");
            }
        }

        //发送回调
        private void onSendCallback(IAsyncResult ar)
        {
            try
            {
                int sent = _socket.EndSend(ar);

                //TODO:处理发送完成后的逻辑
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{_clientId}] 发送回调异常: {ex.Message}");
            }
        }
    }
}
