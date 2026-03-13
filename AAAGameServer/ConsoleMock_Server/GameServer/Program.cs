namespace GameServer
{
    public class Program
    {
        static Server server;

        static void Main(string[] args)
        {
            // 0.0.0.0 监听所有网卡，方便Unity连接（不要用127.0.0.1，否则手机连不上）
            Server server = new Server("0.0.0.0", 8888);

            Console.WriteLine("服务器运行中，按任意键停止...");
            Console.ReadKey();

            // 清理（可选）
            Console.WriteLine("服务器已关闭");

        }
    }
}