/*
 * ┌──────────────────────────────────┐
 * │  描    述: 公告栏Item
 * │  类    名: NoticeItem.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Module.GameUI.Bulletin
{
    public class NoticeItem : MonoBehaviour
    {
        private TextMeshProUGUI _txtTitle; 
        private Toggle  _toggle;
        
        private BulletinInitOnStart _manager;

        private void Awake()
        {
            _txtTitle = GetComponentInChildren<TextMeshProUGUI>(true);
            _toggle = GetComponent<Toggle>();
        }

        void ScrollCellIndex(int idx)
        {
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
            
            //处理toggle
            _toggle.group = GetComponentInParent<ToggleGroup>();
            if(idx == 0)
                _toggle.isOn = true;
        }
    }
}