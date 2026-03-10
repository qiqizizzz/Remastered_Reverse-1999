/*
* ┌──────────────────────────────────┐
* │  描    述: 加载界面控制器
* │  类    名: LoadingController.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common;
using Common.Defines;
using Module.View;
using MVC;
using MVC.Controller;
using MVC.Model;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Module.Loading
{
    public class LoadingController : BaseController
    {
        private AsyncOperation asyncOp;
        private float delayTime = 2f;//延迟时间
        
        public LoadingController() : base()
        {
            GameApp.ViewManager.Register(ViewType.LoadingView,new ViewInfo()
            {
                PrefabName = AddressDefines.UI_LoadingView,
                parentTf = GameApp.ViewManager.canvasTf,
                controller = this
            });
            //ToDo：开始加载的LoadingView - StartLoadingView
            
            InitModuleEvent();
        }

        public override void InitModuleEvent()
        {
            RegisterFunc(EventDefines.LoadingScene, loadSceneCallback);
        }

        //加载场景回调
        private void loadSceneCallback(System.Object[] args)
        {
            //这里默认传入的第一个参数是LoadingModel
            LoadingModel model = args[0] as LoadingModel;
            
            SetModel(model);
            
            //TODO:后面需要区分游戏内还是游戏外加载
            GameApp.ViewManager.Open(ViewType.LoadingView);
            asyncOp = SceneManager.LoadSceneAsync(model.SceneName);

            syncProcess();//这里也要区分
            
            asyncOp.completed += onLoadEndCallback;
        }

        //加载后回调
        private void onLoadEndCallback(AsyncOperation op)
        {
            asyncOp.completed -= onLoadEndCallback;
            
            //延迟回调
            GameApp.TimerManager.Register(delayTime, delegate()
            {
                GetModel<LoadingModel>().callback?.Invoke();
                GameApp.ViewManager.Close(ViewType.LoadingView);
            });
        }

        //同步进度条
        private void syncProcess()
        {
            asyncOp.allowSceneActivation = false;//禁止场景自动切换
            var view = GameApp.ViewManager.GetView<LoadingView>(ViewType.LoadingView);
            view.InitLoading(asyncOp);
        }
    }
}