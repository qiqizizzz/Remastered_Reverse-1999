/*
* ┌──────────────────────────────────┐
* │  描    述: 统一定义游戏内的管理器                      
* │  类    名: GameApp.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common;
using Config;
using DefaultNamespace;
using Module.Character;
using Module.fight.CardMgr;
using Module.Timer;
using MVC;
using Network;
using Sound;

public class GameApp : Singleton<GameApp>
{
    public static ControllerManager ControllerManager;
    public static ViewManager ViewManager;
    public static TimerManager TimerManager;
    public static NetworkManager NetworkManager;
    public static GameDataManager GameDataManager;
    public static MessageCenter MessageCenter;
    public static SoundManager SoundManager;
    public static ConfigManager ConfigManager;
    public static EntityManager EntityManager;
    public static CardManager CardManager;
    
    public override void Init()
    {
        ControllerManager = new ControllerManager();
        ViewManager = new ViewManager();
        TimerManager = new TimerManager();
        NetworkManager = new NetworkManager();
        GameDataManager = new GameDataManager();
        MessageCenter = new MessageCenter();
        SoundManager = new SoundManager();
        ConfigManager = new ConfigManager();
        EntityManager = new EntityManager();
        CardManager = new CardManager();
    }

    public override void Update(float dt)
    {
        TimerManager.OnUpdate(dt);
        NetworkManager.OnUpdate();
    }

    public override void Destroy()
    {
        ControllerManager.Destroy();
    }
}
