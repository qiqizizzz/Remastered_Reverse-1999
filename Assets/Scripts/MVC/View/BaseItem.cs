/*
* ┌────────────────────────────────────────────────────────────────┐
* │  描    述: UI子组件基类(用于卡牌、道具Item等轻量级UI,不受控制器控制)                       
* │  类    名: BaseItem.cs       
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────────────────┘
*/

using System.Collections.Generic;
using UnityEngine;

namespace MVC.View
{
    public class BaseItem : MonoBehaviour
    {
        protected Dictionary<string, GameObject> m_cache_gos = new Dictionary<string, GameObject>();

        #region 生命周期函数
        private void Awake()
        {
            OnAwake();
        }
        
        private void Start()
        {
            OnStart();
        }

        private void Update()
        {
            OnUpdate();
        }
        
        protected virtual void OnAwake()
        {
            
        }

        protected virtual void OnStart()
        {
            
        }

        protected virtual void OnUpdate()
        {
            
        }
        
        protected virtual void OnDestroy()
        {
            
        }
        #endregion
        
        public virtual void SetVisible(bool isVisible)
        {
            if (gameObject.activeSelf != isVisible)
            {
                gameObject.SetActive(isVisible);
            }
        }
        
        public GameObject Find(string res, Transform parent = null)
        {
            if (m_cache_gos.ContainsKey(res))
                return m_cache_gos[res];

            m_cache_gos.Add(res, (parent != null ? parent : transform).Find(res).gameObject);
            return m_cache_gos[res];
        }

        public T Find<T>(string res, Transform parent = null) where T : Component
        {
            GameObject obj = Find(res, parent);
            return obj.GetComponent<T>();
        }
    }
}