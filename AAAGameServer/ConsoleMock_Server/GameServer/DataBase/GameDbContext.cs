using GameServer.DataBase.Entity;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer.DataBase
{
    internal class GameDbContext : DbContext
    {
        public DbSet<User> Users { get; set; } 
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Friend> Friends { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connBuilder = new MySqlConnectionStringBuilder
            {
                Server = "127.0.0.1",
                Database = "game_db",
                UserID = "root",
                Password = "wq175201314",
                Pooling = true,
                MinimumPoolSize = 5,
                MaximumPoolSize = 100
            };

            var serverVersion = ServerVersion.AutoDetect(connBuilder.ConnectionString);//自动检测MySQL版本
            optionsBuilder.UseMySql(connBuilder.ConnectionString, serverVersion);
        }
    }
}
