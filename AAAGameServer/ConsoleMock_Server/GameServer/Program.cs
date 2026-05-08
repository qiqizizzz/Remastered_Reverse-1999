using Network;
using Network.DataBase;
using GameServer.Battle;
using GameServer.Battle.Data;

namespace GameServer
{
    public class Program
    {
        static Server server;

        static void Main(string[] args)
        {
            var configManager = new ConfigManager();
            configManager.LoadAll(Path.Combine(AppContext.BaseDirectory, "Battle", "Data", "json"));

            var battleManager = new BattleManager(configManager);
            Server server = new Server("0.0.0.0", 8888, battleManager);

            Console.WriteLine("服务器运行中，按任意键停止...");
            DBManager.TestConnection();
            Console.ReadKey();

            Console.WriteLine("服务器已关闭");

        }
    }
}