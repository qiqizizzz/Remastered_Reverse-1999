/*
 * ┌──────────────────────────────────┐
 * │  描    述: 公告栏Item
 * │  类    名: NoticeItem.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Module.GameUI.Bulletin
{
    public class NoticeItem : MonoBehaviour
    {
        [Header("UI组件")]
        private TextMeshProUGUI _txtTitle; 
        private Toggle  _toggle;
        private GameObject _redDotObj;

        [Header("数据信息")] 
        private int _idx;
        private string _currentDotPath;
        private Action<int> _onRedDotChanged;
        
        private BulletinInitOnStart _manager;

        private void Awake()
        {
            _txtTitle = GetComponentInChildren<TextMeshProUGUI>(true);
            _toggle = GetComponent<Toggle>();
            _redDotObj = transform.Find("Img_RedDot").gameObject;
            
            _toggle.onValueChanged.AddListener(onToggleValueChanged);
            _onRedDotChanged = OnRedDotChanged;
        }

        private void onToggleValueChanged(bool value)
        {
            GameApp.RedDotManager.SetNodeValue($"More/Bulletin/Event/{_idx}",0);
        }
        
        private void OnRedDotChanged(int count)
        {
            if (_redDotObj != null)
            {
                _redDotObj.SetActive(count > 0);
            }
        }
        
        void ScrollCellIndex(int idx)
        {
            _idx = idx;
            gameObject.name = "NoticeItem_" + idx.ToString();

            if (_manager == null)
            {
                _manager = GetComponentInParent<BulletinInitOnStart>();
            }

            if (_manager != null && idx < _manager.realNoticeDataList.Count)
            {
                if (_txtTitle != null)
                {
                    _txtTitle.text = _manager.realNoticeDataList[idx];
                }
            }
            
            //动态绑定红点
            if (_redDotObj != null)
            {
                string newDotPath = $"More/Bulletin/Event/{idx}";

                if (!string.IsNullOrEmpty(_currentDotPath) && _currentDotPath != newDotPath)
                {
                    GameApp.RedDotManager.UnregisterCallback(_currentDotPath, _onRedDotChanged);
                }
                
                _currentDotPath = newDotPath;
                GameApp.RedDotManager.RegisterCallback(_currentDotPath, _onRedDotChanged);
            }
            
            //处理toggle
            _toggle.group = GetComponentInParent<ToggleGroup>();
            if(idx == 0)
                _toggle.isOn = true;
        }
        
        private void OnDestroy()
        {
            if (!string.IsNullOrEmpty(_currentDotPath))
            {
                GameApp.RedDotManager.UnregisterCallback(_currentDotPath, _onRedDotChanged);
            }
        }
    }
}