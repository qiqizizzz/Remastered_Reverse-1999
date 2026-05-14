/*
* ┌───────────────────────────────────────────────────────────────────┐
* │  描    述: 继承mono的脚本，需要挂载游戏物体，跳转场景后当前脚本物体不删除                      
* │  类    名: GameScene.cs       
* │  创    建: By qiqizizzz
* └───────────────────────────────────────────────────────────────────┘
*/

using System;
using System.Threading.Tasks;
using Module.Game;
using Module.chat;
using Module.fight;
using Module.GameUI;
using Module.Inventory;
using Module.level;
using Module.Loading;
using Module.Cultivation;
using MVC;
using Common;
using UnityEngine;

namespace DefaultNamespace
{
    public class GameScene : MonoBehaviour
    {
        public Texture2D commonCursor;// 鼠标指针图片
        private float dt;
        private static GameScene _instance;
        private bool isAppInitialized = false; 
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            GameApp.Instance.Init();
            Application.runInBackground = true;
            
        }

        private async void Start()
        {
            if(_instance != this) return;
            
            Cursor.SetCursor(commonCursor, Vector2.zero, CursorMode.Auto);
            
            await RegisterConfigs();
            RegisterModules();
            InitModules();

            isAppInitialized = true;
            
            QLog.Info("<color=cyan>游戏所有模块与配置加载完毕，游戏正式开始！</color>");
        }

        private void Update()
        {
            if (!isAppInitialized) return;
            
            dt = Time.deltaTime;
            GameApp.Instance.Update(dt);
        }
        
        private void OnDestroy()
        {
            if(_instance == this)
                GameApp.Instance.Destroy();
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
            GameApp.ControllerManager.Register(ControllerType.Inventory, new InventoryController());
            GameApp.ControllerManager.Register(ControllerType.CultivationController, new CultivationController());
            GameApp.ControllerManager.Register(ControllerType.Fight, new FightController());
        }
        
        // 初始化所有模块
        private void InitModules()
        {
            GameApp.ControllerManager.Init();
        }
        
        // 注册配置表
        private async Task RegisterConfigs()
        {
            await GameApp.ConfigManager.LoadAllConfigsAsync();
        }
    }
}