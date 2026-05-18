/*
* ┌──────────────────────────────────┐
* │  描    述: 统一定义游戏内的管理器(组合根)                      
* │  类    名: GameApp.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common;
using Config;
using DefaultNamespace;
using Module.Character;
using Module.Effect;
using Module.fight.CardMgr;
using Module.Matchmaking;
using Module.RedDot;
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
    public static EffectManager EffectManager;
    public static PvpSession PvpSession;
    public static RedDotManager  RedDotManager;
    
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
        EffectManager = new EffectManager();
        PvpSession = new PvpSession();
        RedDotManager = new RedDotManager();
    }

    public override void Update(float dt)
    {
        TimerManager.OnUpdate(dt);
        NetworkManager.OnUpdate();
        EffectManager.OnUpdate();
    }

    public override void Destroy()
    {
        ControllerManager.Destroy();
        RedDotManager.Destroy();
    }
}
