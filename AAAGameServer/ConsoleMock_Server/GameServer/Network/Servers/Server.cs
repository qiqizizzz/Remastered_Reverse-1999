using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using GameServer.Battle;

namespace Network
{
    //服务端
    internal class Server
    {
        private string _ip;//ip地址
        private int _port;//端口号
        private Socket _socket;
        private const int backlog = 1024;//最大连接数

        // 管理所有连接的客户端
        private Dictionary<string, Client> _clients = new Dictionary<string, Client>();
        // 存储在线玩家
        private Dictionary<string, Client> _userClients = new Dictionary<string, Client>();

        private int _clientIdCounter = 0;//计数器
        private readonly object _lockObj = new object();

        public BattleManager BattleManager { get; }

        public Server(string ip,int port, BattleManager battleManager)
        {
            _ip = ip;
            _port = port;
            BattleManager = battleManager;

            Connect();
        }

        //连接服务器
        private void Connect()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(_ip), _port);
            _socket.Bind(iPEndPoint);
            _socket.Listen(backlog);

            Console.WriteLine($"服务器已启动，监听IP：{_ip}，端口：{_port}");

            BeginAccept();

        }

        //开始接收客户端连接
        private void BeginAccept()
        {
            try
            {
                _socket.BeginAccept(onBeginAcceptCallback, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"监听异常:{ ex.Message}");
            }
        }

        private void onBeginAcceptCallback(IAsyncResult ar)
        {
            try
            {
                Socket clientSocket = _socket.EndAccept(ar);
                string clientId = $"Client_{++_clientIdCounter}_{Guid.NewGuid().ToString("N").Substring(0, 6)}";
                Console.WriteLine($"[{clientId}] 新客户端连接: {clientSocket.RemoteEndPoint}");

                var client = new Client(clientSocket, clientId ,this);

                //加入管理 - 这里的锁会自动赋值与清空
                lock(_lockObj)
                {
                    _clients[clientId] = client;
                }

                BeginAccept();//继续接收
            }catch(Exception ex)
            {
                Console.WriteLine($"接收连接异常,{ex.Message}");
                BeginAccept();//出错也继续监听
            }
        }

        //广播消息给所有客户端
        public void BroadCast(byte[] data)
        {
            lock (_lockObj)
            {
                foreach(var client in _clients.Values)
                {
                    client.Send(data);
                }
            }
        }

        //绑定身份 - 玩家登录成功后调用
        public void AddUserClient(string username, Client client)
        {
            lock(_lockObj)
            {
                _userClients[username] = client;
                Console.WriteLine($"[Server] 玩家 '{username}' 已绑定在线字典");
            }
        }

        public Client GetClientByUsername(string username)
        {
            lock(_lockObj)
            {
                return _userClients.TryGetValue(username, out Client client) ? client : null;
            }
        }

        //获取客户端
        public Client GetClientByClientId(string clientId)
        {
            lock(_lockObj)
            {
                return _clients.TryGetValue(clientId, out Client client) ? client : null;
            }
        }

        //移除客户端 - Client断开时调用
        public void RemoveClient(string clientId, string username)
        {
            lock(_lockObj)
            {
                if(!string.IsNullOrEmpty(username) && _userClients.ContainsKey(username))
                {
                    _userClients.Remove(username);
                    Console.WriteLine($"[Server] 玩家 '{username}' 从在线字典移除");
                }


                if(_clients.ContainsKey(clientId))
                {
                    _clients.Remove(clientId);
                    Console.WriteLine($"[{clientId}]从管理列表移除");
                }
            }
        }


    }
}
