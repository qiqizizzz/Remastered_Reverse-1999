/*
* ┌──────────────────────────────────┐
* │  描    述: 统一定义游戏内的管理器                      
* │  类    名: GameApp.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common;
using DefaultNamespace;
using Module.Timer;
using MVC;
using Network;

public class GameApp : Singleton<GameApp>
{
    public static ControllerManager ControllerManager;
    public static ViewManager ViewManager;
    public static TimerManager TimerManager;
    public static NetworkManager NetworkManager;
    public static GameDataManager GameDataManager;
    public static MessageCenter MessageCenter;
        
    public override void Init()
    {
        ControllerManager = new ControllerManager();
        ViewManager = new ViewManager();
        TimerManager = new TimerManager();
        NetworkManager = new NetworkManager();
        GameDataManager = new GameDataManager();
        MessageCenter = new MessageCenter();
    }

    public override void Update(float dt)
    {
        TimerManager.OnUpdate(dt);
        NetworkManager.OnUpdate();
    }
}
