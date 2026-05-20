/*
* ┌──────────────────────────────────┐
* │  描    述: 公告无限列表                      
* │  类    名: BulletinInitOnStart.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Module.GameUI.Bulletin
{
    [RequireComponent(typeof(UnityEngine.UI.LoopScrollRect))]
    [DisallowMultipleComponent]
    public class BulletinInitOnStart : MonoBehaviour, LoopScrollPrefabSource, LoopScrollDataSource
    {
        public GameObject item;
        public int totalCount = 11;

        // Implement your own Cache Pool here. The following is just for example.
        private readonly Stack<Transform> pool = new Stack<Transform>();
        
        public List<string> realNoticeDataList = new List<string>();

        private void Awake()
        {
            InitBulletinData(realNoticeDataList);
        }
        
        void Start()
        {
            var ls = GetComponent<LoopScrollRect>();
            ls.prefabSource = this;
            ls.dataSource = this;
            ls.totalCount = totalCount;
            ls.RefillCells();
        }

        private void InitBulletinData(List<string> data)
        {
            realNoticeDataList = data; 
            
            var ls = GetComponent<LoopScrollRect>();
            ls.prefabSource = this;
            ls.dataSource = this;
            ls.totalCount = realNoticeDataList.Count; 
            
            ls.RefillCells();
        }
        
        public GameObject GetObject(int index)
        {
            if (pool.Count == 0)
            {
                return Instantiate(item);
            }
            Transform candidate = pool.Pop();
            candidate.gameObject.SetActive(true);
            return candidate.gameObject;
        }

        public void ReturnObject(Transform trans)
        {
            // Use `DestroyImmediate` here if you don't need Pool
            trans.SendMessage("ScrollCellReturn", SendMessageOptions.DontRequireReceiver);
            trans.gameObject.SetActive(false);
            trans.SetParent(transform, false);
            pool.Push(trans);
        }

        public void ProvideData(Transform tr, int idx)
        {
            tr.SendMessage("ScrollCellIndex", idx);
        }

        
    }
}