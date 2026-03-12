/*
* ┌───────────────────────────────────────────────────────────────────┐
* │  描    述: 继承mono的脚本，需要挂载游戏物体，跳转场景后当前脚本物体不删除                      
* │  类    名: GameScene.cs       
* │  创    建: By qiqizizzz
* └───────────────────────────────────────────────────────────────────┘
*/

using System;
using Common;
using Module.GameUI;
using Module.Loading;
using MVC;
using UnityEngine;

namespace DefaultNamespace
{
    public class GameScene : MonoBehaviour
    {
        public Texture2D commonCursor;// 鼠标指针图片
        
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            GameApp.Instance.Init();
        }

        private void Start()
        {
            Cursor.SetCursor(commonCursor, Vector2.zero, CursorMode.Auto);
            
            RegisterConfigs();
            RegisterModules();
            InitModules();

            ConnectToServer(); //连接服务器
        }

        private void Update()
        {
            float dt = Time.deltaTime;
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