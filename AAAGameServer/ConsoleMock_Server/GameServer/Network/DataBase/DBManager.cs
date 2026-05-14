
using GameServer.Common;
using Network.DataBase.Entity;

namespace Network.DataBase
{
    internal class DBManager
    {
        #region 登录与连接相关

        //测试数据库连接
        public static bool TestConnection()
        {
            try
            {
                using (var db = new GameDbContext())
                {
                    bool canConnect = db.Database.CanConnect();
                    if (canConnect)
                    {
                        QLog.Info("[DBManager] 数据库连接成功");
                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                QLog.Info($"[DBManager] 数据库连接失败: {ex.Message}");
                return false;
            }
        }

        //注册账号
        public static bool Register(string username, string password)
        {
            using (var db = new GameDbContext())
            {
                bool exists = db.Users.Any(u => u.Username == username);
                if (exists) return false;

                var newUser = new User
                {
                    Username = username,
                    Password = password,
                    RegisterTime = DateTime.Now,
                    Is_banned = false
                };

                db.Users.Add(newUser);
                db.SaveChanges();
                return true;
            }
        }

        //登录账号
        public static int Login(string username, string password)
        {
            using (var db = new GameDbContext())
            {
                var user = db.Users.FirstOrDefault(u => u.Username == username);//找出第一个匹配的
                if (user == null) return -1;//用户不存在
                if (user.Is_banned) return -2;//账号被封禁
                if (user.Password == password) return 1;//密码匹配

                return -1;//密码错误
            }
        }

        //更新最后登录时间
        public static void UpdateLastLoginTime(string username)
        {
            using(var db = new GameDbContext())
            {
                var user = db.Users.FirstOrDefault(u => u.Username == username);
                if (user != null)
                {
                    user.LastLoginTime = DateTime.Now;
                    db.SaveChanges();
                }
            }
        }

        #endregion

        #region 聊天系统相关
        // 保存聊天消息
        public static void SaveChatMessage(string senderName, string receiverName, string content)
        {
            using (var db = new GameDbContext())
            {
                var chatMessage = new ChatMessage
                {
                    SenderName = senderName,
                    ReceiverName = receiverName,
                    Content = content,
                    SendTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() // 使用 UTC 时间戳
                };
                db.ChatMessages.Add(chatMessage);
                db.SaveChanges();
            }
        }
        // 获取聊天记录
        public static List<ChatMessage> GetChatHistory(string user1, string user2, int limit = 50)
        {
            using (var db = new GameDbContext())
            {
                var messages = db.ChatMessages
                    .Where(m => (m.SenderName == user1 && m.ReceiverName == user2) ||
                                (m.SenderName == user2 && m.ReceiverName == user1))
                    .OrderByDescending(m => m.SendTime)
                    .Take(limit)
                    .ToList();

                messages.Reverse();

                return messages;
            }
        }

        #region 好友系统相关

        //获取玩家好友列表
        public static List<Friend> GetFriends(string username)
        {
            using (var db = new GameDbContext())
            {
                return db.Friends.Where(f => f.Username == username).ToList();
            }
        }
        public static List<string> GetFriendsName(string username)
        {
            using (var db = new GameDbContext())
            {
                return db.Friends.Where(f => f.Username == username)
                    .Select(f=> f.Username)
                    .ToList();
            }
        }

        public static bool AddFriend(string userName, string friendUserName)
        {
            // 不能为空、不能自己加自己
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(friendUserName) || userName == friendUserName)
            {
                return false;
            }

            using (var db = new GameDbContext())
            {
                // 开启事务，确保双向添加要么都成功，要么都回滚
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        // 检查是否已经是好友（双向检查）
                        bool alreadyFriends = db.Friends.Any(f =>
                            (f.Username == userName && f.FriendUsername == friendUserName) ||
                            (f.Username == friendUserName && f.FriendUsername == userName)
                        );

                        if (alreadyFriends) return false;

                        var now = DateTime.Now;

                        // 添加 A->B 的关系
                        db.Friends.Add(new Friend
                        {
                            Username = userName,
                            FriendUsername = friendUserName,
                            CreateTime = now
                        });

                        // 添加 B->A 的关系（互相好友）
                        db.Friends.Add(new Friend
                        {
                            Username = friendUserName,
                            FriendUsername = userName,
                            CreateTime = now
                        });

                        db.SaveChanges();
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }

        //删除好友
        public static bool RemoveFriend(string userName, string friendUserName)
        {
            using (var db = new GameDbContext())
            {
                var friends = db.Friends.Where(f =>
                    (f.Username == userName && f.FriendUsername == friendUserName) ||
                    (f.FriendUsername == userName && f.Username == friendUserName))
                    .ToList();

                if (!friends.Any()) return false;

                db.Friends.RemoveRange(friends);
                db.SaveChanges();
                return true;
            }
        }

        //搜索玩家 - 模糊查询
        public static List<string> SearchUser(string key)
        {
            using (var db = new GameDbContext())
            {
                return db.Users
                    .Where(u => u.Username.Contains(key))
                    .Select(u => u.Username) //只查询用户名
                    .Distinct()
                    .Take(20)
                    .ToList();
            }
        }
        #endregion

        #endregion
    }
}
