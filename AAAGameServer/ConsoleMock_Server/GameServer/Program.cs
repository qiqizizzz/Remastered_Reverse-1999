namespace GameServer
{
    public class Program
    {
        static Server server;

        static void Main(string[] args)
        {
            server = new Server("127.0.0.1", 1001);
            
        }
    }
}