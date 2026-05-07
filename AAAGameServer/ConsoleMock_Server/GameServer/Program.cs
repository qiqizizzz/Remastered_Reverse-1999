using Network;
using Network.DataBase;

namespace GameServer
{
    public class Program
    {
        static Server server;

        static void Main(string[] args)
        {
            Server server = new Server("0.0.0.0", 8888);

            Console.WriteLine("服务器运行中，按任意键停止...");
            DBManager.TestConnection();
            Console.ReadKey();

            Console.WriteLine("服务器已关闭");

        }
    }
}