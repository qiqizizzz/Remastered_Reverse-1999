/*
* ┌──────────────────────────────────┐
* │  描    述: 统一定义游戏内的管理器                      
* │  类    名: GameApp.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common;
using Module.Timer;
using MVC;

public class GameApp : Singleton<GameApp>
{
    public static ControllerManager ControllerManager;
    public static ViewManager ViewManager;
    public static TimerManager TimerManager;
        
    public override void Init()
    {
        ControllerManager = new ControllerManager();
        ViewManager = new ViewManager();
        TimerManager = new TimerManager();
    }

    public override void Update(float dt)
    {
        TimerManager.OnUpdate(dt);
    }
}
    
