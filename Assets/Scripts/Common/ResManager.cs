/*
* ┌──────────────────────────────────┐
* │  描    述: 资源加载/卸载管理器                      
* │  类    名: ResManager.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
* 对于特效、怪物、道具等重复使用的对象使用对象池,UI界面则由UI界面那边管理
*/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Common
{
    public static class ResManager
    {
        private static readonly Dictionary<string, Queue<GameObject>> _pool;

        static ResManager()
        {
            _pool = new Dictionary<string, Queue<GameObject>>();
        }

        //同步加载实例
        public static GameObject Instantiate(string keyName, Transform parent = null)
        {
            GameObject go = Addressables.InstantiateAsync(keyName, parent).WaitForCompletion();
            go.name = keyName;
            return go;
        }
        
        //异步加载实例
        public static void InstantiateAsync(string keyName, Action<GameObject> onCompleted, Transform parent = null)
        {
            Addressables.InstantiateAsync(keyName, parent).Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)//注册回调,成功了则传回实例
                {
                    handle.Result.name = keyName;
                    onCompleted?.Invoke(handle.Result);
                }
                else
                {
                    onCompleted?.Invoke(null);
                }
            };
        }
        
        //卸载实例
        public static bool UnLoadInstance(GameObject go)
        {
            return Addressables.ReleaseInstance(go);
        }

        #region 游戏asset
        //同步加载assets
        public static T LoadAsset<T>(string assetName) where T : UnityEngine.Object
        {
            return Addressables.LoadAssetAsync<T>(assetName).WaitForCompletion();
        }
        
        //异步加载assets
        public static void LoadAssetAsync<T>(string assetName, Action<T> onCompleted) where T : UnityEngine.Object
        {
            Addressables.LoadAssetAsync<T>(assetName).Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    onCompleted?.Invoke(handle.Result);
                }
                else
                {
                    onCompleted?.Invoke(null);
                }
            };
        }

        //同步加载指定key的所有资源
        public static IList<T> LoadAssets<T>(string keyName,
            out AsyncOperationHandle<IList<T>> handle,
            Action<T> callBackOnEveryOne = null) where T : UnityEngine.Object
        {
            handle = Addressables.LoadAssetsAsync<T>(keyName, callBackOnEveryOne, true);
            return handle.WaitForCompletion();
        }
        
        //异步加载指定key的所有资源
        public static void LoadAssetAsync<T>(string keyName, Action<AsyncOperationHandle<IList<T>>> callBack,
            Action<T> callBackOnEveryOne = null) where T : UnityEngine.Object
        {
            Addressables.LoadAssetsAsync<T>(keyName, callBackOnEveryOne).Completed += callBack;
        }
        
        //卸载assets
        public static void UnLoadAsset<T>(T obj)
        {
            if (obj != null)
                Addressables.Release(obj);
        }
        
        //批量卸载
        public static void UnLoadAssetsHandle<TObject>(AsyncOperationHandle<TObject> handle)
        {
            if (handle.IsValid())
                Addressables.Release(handle);
        }
        public static void UnloadAssetsHandle<T>(AsyncOperationHandle<IList<T>> handle)
        {
            if (handle.IsValid())
                Addressables.Release(handle);
        }
        #endregion

        #region 对象池
        //同步从对象池加载实例
        public static GameObject InstantiateFromPool(string keyName, Transform parent = null)
        {
            if (_pool.ContainsKey(keyName))
            {
                while (_pool[keyName].Count > 0)
                {
                    GameObject obj = _pool[keyName].Dequeue();
                    if (obj == null) continue;
                
                    obj.SetActive(true);
                    if(parent != null)
                        obj.transform.SetParent(parent);
                    return obj;
                }
            }

            return Instantiate(keyName, parent);
        }
        
        //异步从对象池加载实例
        public static void InstantiateFromPoolAsync(string keyName, Action<GameObject> onCompleted,
            Transform parent = null)
        {
            if (_pool.ContainsKey(keyName))
            {
                while (_pool[keyName].Count > 0)
                {
                    GameObject obj = _pool[keyName].Dequeue();
                    if (obj == null) continue;
                    
                    obj.SetActive(true);
                    if(parent != null)
                        obj.transform.SetParent(parent);
                    onCompleted?.Invoke(obj);
                    return;
                }
            }
            
            InstantiateAsync(keyName, onCompleted, parent);
        }
        
        //释放实例到对象池
        public static void ReleaseToPool(string keyName, GameObject obj, int maxPoolSize = 20)
        {
            if (obj == null) return;

            obj.SetActive(false);
            obj.transform.SetParent(null); 

            if (!_pool.ContainsKey(keyName))
                _pool[keyName] = new Queue<GameObject>();

            if (_pool[keyName].Count < maxPoolSize)
            {
                _pool[keyName].Enqueue(obj);
            }
            else
            {
                // 超出池容量，直接释放
                Addressables.ReleaseInstance(obj);
            }
        }
        
        //清理单个对象池
        public static void ClearPool(string keyName)
        {
            if (_pool.TryGetValue(keyName, out var queue))
            {
                while (queue.Count > 0)
                {
                    var obj = queue.Dequeue();
                    Addressables.ReleaseInstance(obj);
                }
                _pool.Remove(keyName);
            }
        }
        
        //清理所有对象池
        public static void ClearAllPools()
        {
            foreach (var key in _pool.Keys)
                ClearPool(key);
            _pool.Clear();
        }
        #endregion
    }
}