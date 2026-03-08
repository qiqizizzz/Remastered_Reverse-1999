/*
* ┌──────────────────────────────────┐
* │  描    述: 视图管理器                      
* │  类    名: ViewManager.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using System.Linq;
using MVC.Controller;
using MVC.View;
using UnityEngine;
using UnityEngine.UI;

namespace MVC
{
    /// <summary>
    /// 视图信息类
    /// PrefabName: 视图预制体名称
    /// ParentTf: 视图父物体
    /// Controller: 视图对应的控制器
    /// Sorting_Order: 视图的渲染层级
    /// </summary>
    public class ViewInfo
    {
        public string PrefabName;
        public Transform parentTf;
        public BaseController controller;
        public int Sorting_Order;
    }
    
    public class ViewManager
    {
        public Transform canvasTf;
        public Transform worldCanvasTf;
        private Dictionary<int,IBaseView> _opens;
        private Dictionary<int, IBaseView> _viewCache;
        private Dictionary<int, ViewInfo> _viewInfos;//注册视图信息
        
        public ViewManager()
        {
            canvasTf = GameObject.Find("Canvas").transform;
            worldCanvasTf = GameObject.Find("WorldCanvas").transform;
            _opens = new Dictionary<int, IBaseView>();
            _viewCache = new Dictionary<int, IBaseView>();
            _viewInfos = new Dictionary<int, ViewInfo>();
        }

        public bool IsOpen(int key) => _opens.ContainsKey(key);
        
        public void Register(int key, ViewInfo viewInfo)
        {
            if(!_viewInfos.ContainsKey(key))
                _viewInfos.Add(key, viewInfo);
        }

        public void Register(ViewType type, ViewInfo viewInfo)
        {
            Register((int)type, viewInfo);
        }

        public void UnRegister(int key)
        {
            if (_viewInfos.ContainsKey(key))
                _viewInfos.Remove(key);
        }

        public void RemoveView(int key)
        {
            _opens.Remove(key);
            _viewCache.Remove(key);
            _viewInfos.Remove(key);
        }

        public void RemoveControllerView(BaseController ctl)
        {
            foreach (var item in _viewInfos)
            {
                if (item.Value.controller == ctl)
                    RemoveView(item.Key);
            }
        }

        public IBaseView GetView(int key)
        {
            if (_opens.ContainsKey(key))
            {
                return _opens[key];
            }

            if (_viewCache.ContainsKey(key))
            {
                return _viewCache[key];
            }

            return null;
        }

        public T GetView<T>(int key) where T : class, IBaseView
        {
            IBaseView view = GetView(key);
            if (view != null)
            {
                return view as T;
            }

            return null;
        }

        public void DestroyView(int key)
        {
            IBaseView oldView = GetView(key);
            if (oldView != null)
            {
                UnRegister(key);
                oldView.DestroyView();
                _viewCache.Remove(key);
            }
        }
        
        //关闭面板视图
        public void Close(int key, params object[] args)
        {
            if(IsOpen(key) == false) return;
            
            IBaseView view = GetView(key);
            if (view != null)
            {
                _opens.Remove(key);
                view.Close(args);
                _viewInfos[key].controller.CloseView(view);
            }
        }

        public void CloseAll()
        {
            List<IBaseView> list = _opens.Values.ToList();
            for (int i = list.Count - 1; i >= 0; i--)
            {
                Close(list[i].ViewId);
            }
        }
        
        //打开某个视图面板
        public void Open(int key, params object[] args)
        {
            IBaseView view = GetView(key);
            ViewInfo viewInfo = _viewInfos[key];
            if (view == null)
            {
                //若视图不存在,则进行资源加载
                string type = ((ViewType)key).ToString();
                GameObject uiObj = UnityEngine.Object.Instantiate(Resources.Load($"View/{viewInfo.PrefabName}"), 
                    viewInfo.parentTf) as GameObject;

                Canvas canvas = uiObj.GetComponent<Canvas>();
                if (canvas == null)
                {
                    canvas = uiObj.AddComponent<Canvas>();
                }

                if (uiObj.GetComponent<GraphicRaycaster>() == null)
                {
                    uiObj.AddComponent<GraphicRaycaster>();
                }
                canvas.overrideSorting = true;//可以设置层级
                canvas.sortingOrder = viewInfo.Sorting_Order;//设置层级
                
                /* 另一种解决方案：
                 * string typeName = $"Module.{type.ToString()}";
                 * Type scriptType = Type.GetType(typeName);
                 * view = uiObj.AddComponent(viewType) as IBaseView;
                 * 简单来说就是需要加入命名空间 Module才能查找到对应的脚本
                 */
                
                Type viewType = FindType(type); // 使用新方法查找类型
                view = uiObj.AddComponent(viewType) as IBaseView;//添加对应View脚本
                view.ViewId = key;//设置视图id
                view.Controller = viewInfo.controller;//设置控制器
                
                //添加到视图缓存
                _viewCache.Add(key, view);
                viewInfo.controller.OnLoadView(view);
            }
            
            //已经打开了
            if(this._opens.ContainsKey(key) == true) return;
            this._opens.Add(key, view);
            
            //已经初始化过
            if (view.IsInit())
            {
                view.SetVisible(true);//显示
                view.Open(args);//打开
                viewInfo.controller.OpenView(view);
            }
            else
            {
                view.InitUI();
                view.InitData();
                view.Open(args);
                viewInfo.controller.OpenView(view);
            }
        }
        public void Open(ViewType viewType, params object[] args)
        {
            Open((int)viewType, args);
        }
        
        //查找对应类型的脚本
        private Type FindType(string typeName)
        {
            return AppDomain.CurrentDomain.GetAssemblies() //获取当前应用程序域中所有已加载的程序集
                .SelectMany(assembly => assembly.GetTypes()) //将获取的所有类型扁平化为一个序列
                .FirstOrDefault(type => type.Name == typeName); //寻找匹配名称的第一个类型脚本
        }
        
    }
}