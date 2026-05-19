/*
* ┌──────────────────────────────────┐
* │  描    述: 红点运行时绑定管家 (负责自动注册和注销)                      
* │  类    名: RedDotMonoBinder.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Module.RedDot.Component
{
    public class RedDotMonoBinder : MonoBehaviour
    {
        [Header("烘培好的红点数据(由Editor工具自动生成)")] 
        public List<RedDotBakeData> BakedDataList = new List<RedDotBakeData>();
        
        //内部缓存：记录所有的回调委托
        //使用KeyValuePair 允许同一个面板里有多个红点监听同一个路径
        private List<KeyValuePair<string, Action<int>>> _cachedCallbacks = new List<KeyValuePair<string, Action<int>>>();

        private void Awake()
        {
            BindAllRedDots();
        }

        private void BindAllRedDots()
        {
            if (BakedDataList == null || BakedDataList.Count == 0) return;

            foreach (var data in BakedDataList)
            {
                if (string.IsNullOrEmpty(data.NodePath) || data.DotObj == null)
                {
                    continue;
                }

                Action<int> callback = (count) =>
                {
                    if (data.DotObj != null)
                    {
                        bool isShow = count > 0;
                        if (data.DotObj.activeSelf != isShow)
                        {
                            data.DotObj.SetActive(isShow);
                        }
                    }
                };

                GameApp.RedDotManager.RegisterCallback(data.NodePath, callback);
                
                _cachedCallbacks.Add(new KeyValuePair<string, Action<int>>(data.NodePath, callback));//内部记录
            }
        }

        private void OnDestroy()
        {
            if (_cachedCallbacks != null)
            {
                foreach (var kvp in _cachedCallbacks)
                {
                    string path = kvp.Key;
                    Action<int> callback = kvp.Value;

                    GameApp.RedDotManager.UnregisterCallback(path, callback);
                }

                _cachedCallbacks.Clear();
            }
        }
    }
}