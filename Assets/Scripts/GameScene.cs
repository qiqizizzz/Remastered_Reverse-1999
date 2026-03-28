/*
* ┌───────────────────────────────────────────────────────────────────┐
* │  描    述: 继承mono的脚本，需要挂载游戏物体，跳转场景后当前脚本物体不删除                      
* │  类    名: GameScene.cs       
* │  创    建: By qiqizizzz
* └───────────────────────────────────────────────────────────────────┘
*/

using Common.Defines;
using DefaultNamespace.Module.Game;
using DefaultNamespace.Module.level;
using Module.chat;
using Module.GameUI;
using Module.Loading;
using MVC;
using UnityEngine;

namespace DefaultNamespace
{
    public class GameScene : MonoBehaviour
    {
        public Texture2D commonCursor;// 鼠标指针图片
        private float dt;
        private static bool isLoaded = false;
        
        private void Awake()
        {
            if(isLoaded) Destroy(gameObject);
            else
            {
                isLoaded = true;
                DontDestroyOnLoad(gameObject);
                GameApp.Instance.Init();
                Application.runInBackground = true;
            }
        }

        private void Start()
        {
            Cursor.SetCursor(commonCursor, Vector2.zero, CursorMode.Auto);
            
            RegisterConfigs();
            RegisterModules();
            InitModules();
        }

        private void Update()
        {
            dt = Time.deltaTime;
            GameApp.Instance.Update(dt);
        }

        //连接服务器
        private void ConnectToServer()
        {
            GameApp.NetworkManager.Connect();
        }
        
        // 注册控制器
        private void RegisterModules()
        {
            GameApp.ControllerManager.Register(ControllerType.GameUI, new GameUIController());
            GameApp.ControllerManager.Register(ControllerType.Loading, new LoadingController());
            GameApp.ControllerManager.Register(ControllerType.Game, new GameController());
            GameApp.ControllerManager.Register(ControllerType.Chat, new ChattingController());
            GameApp.ControllerManager.Register(ControllerType.Level, new LevelController());
        }
        
        // 初始化所有模块
        private void InitModules()
        {
            GameApp.ControllerManager.Init();
        }
        
        // 注册配置表
        private void RegisterConfigs()
        {
            //ResManager.InstantiateFromPoolAsync();
        }
    }
}