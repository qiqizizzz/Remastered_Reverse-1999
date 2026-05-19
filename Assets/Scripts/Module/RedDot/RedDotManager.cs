/*
* ┌──────────────────────────────────┐
* │  描    述: 红点管理器                      
* │  类    名: RedDotManager.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using Common;
using Module.RedDot.Component;

namespace Module.RedDot
{
    public class RedDotManager
    {
        private RedDotNode _root;//虚拟根节点
        private const char _separator = '/';

        public RedDotManager()
        {
            _root = new RedDotNode("Root");
            
            QLog.Info("RedDotManager initialized.");
        }

        public void Destroy()
        {
            _root = null;
        }

        //核心寻路方法
        private RedDotNode GetOrAddNode(string path)
        {
            if(string.IsNullOrEmpty(path)) return _root;

            string[] nodeNames = path.Split(_separator);
            RedDotNode currentNode = _root;

            foreach (var name in nodeNames)
            {
                //如果当前节点下没有这个子节点，就创建一个新的子节点并挂载
                if (!currentNode.Children.TryGetValue(name, out RedDotNode childNode))
                {
                    childNode = new RedDotNode(name, currentNode);
                    currentNode.Children.Add(name, childNode);
                }

                currentNode = childNode;
            }

            return currentNode;
        }

        //设置某路径的红点绝对值
        public void SetNodeValue(string path, int newValue)
        {
            if (newValue < 0) newValue = 0;

            RedDotNode node = GetOrAddNode(path);

            int delta = newValue - node.Value;
            
            node.ChangeValue(delta);
        }

        //注册红点监听
        public void RegisterCallback(string path, Action<int> callback)
        {
            if(callback == null) return;
            
            RedDotNode node = GetOrAddNode(path);
            node.OnValueChanged += callback;
            
            callback.Invoke(node.Value);//注册时立即回调当前值，确保UI状态正确
        }

        //注销红点监听
        public void UnregisterCallback(string path, Action<int> callback)
        {
            if (callback == null) return;
            
            RedDotNode node = GetOrAddNode(path);
            node.OnValueChanged -= callback;
        }
    }
}