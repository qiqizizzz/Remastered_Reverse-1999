/*
* ┌───────────────────────────────────────────────────────────────────┐
* │  描    述: 继承mono的脚本，需要挂载游戏物体，跳转场景后当前脚本物体不删除                      
* │  类    名: GameScene.cs       
* │  创    建: By qiqizizzz
* └───────────────────────────────────────────────────────────────────┘
*/

using System;
using Module.GameUI;
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
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            GameApp.Instance.Update(dt);
        }

        // 注册控制器
        private void RegisterModules()
        {
            GameApp.ControllerManager.Register(ControllerType.GameUIController, new GameUIController());
        }
        
        // 初始化所有控制器
        private void InitModules()
        {
            GameApp.ControllerManager.Init();
        }
        
        // 注册配置表
        private void RegisterConfigs()
        {
            
        }
        
    }
}