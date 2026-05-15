using GameProtocol;
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
            RegisterProtocolHandlers();

            var configManager = new ConfigManager();
            configManager.LoadAll(Path.Combine(AppContext.BaseDirectory, "Battle", "Data", "json"));

            var battleManager = new BattleManager(configManager);
            Server server = new Server("0.0.0.0", 8888, battleManager);

            Console.WriteLine("服务器运行中，按任意键停止...");
            DBManager.TestConnection();
            Console.ReadKey();

            Console.WriteLine("服务器已关闭");
        }

        private static void RegisterProtocolHandlers()
        {
            ProtocolRouter.Register(ActionCode.Logon, new LogonHandler());
            ProtocolRouter.Register(ActionCode.Login, new LogonHandler());
            ProtocolRouter.Register(ActionCode.ChatPrivate, new ChatHandler());
            ProtocolRouter.Register(ActionCode.GetChatHistory, new ChatHandler());
            ProtocolRouter.Register(ActionCode.FriendOperation, new FriendHandler());
            ProtocolRouter.Register(ActionCode.Heartbeat, new HeartbeatHandler());
            ProtocolRouter.Register(ActionCode.EnterPve, new BattleHandler());
            ProtocolRouter.Register(ActionCode.PlayCard, new BattleHandler());
            ProtocolRouter.Register(ActionCode.EndTurn, new BattleHandler());
            ProtocolRouter.Register(ActionCode.CommitRound, new BattleHandler());
            ProtocolRouter.Register(ActionCode.MoveCard, new BattleHandler());
            ProtocolRouter.Register(ActionCode.UnDoAction, new BattleHandler());
            ProtocolRouter.Register(ActionCode.RequestBattleState, new BattleHandler());
            ProtocolRouter.Register(ActionCode.JoinPvP, new BattleHandler());
            ProtocolRouter.Register(ActionCode.LeavePvP, new BattleHandler());
        }
    }
}