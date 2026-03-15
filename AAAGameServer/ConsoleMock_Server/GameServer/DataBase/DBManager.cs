using GameServer.DataBase.Entity;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer.DataBase
{
    internal class DBManager
    {
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
                        Console.WriteLine("[DBManager] 数据库连接成功");
                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DBManager] 数据库连接失败: {ex.Message}");
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

    }
}
