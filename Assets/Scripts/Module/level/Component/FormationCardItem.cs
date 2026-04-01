/*
* ┌──────────────────────────────────┐
* │  描    述: 编队界面的单个角色卡牌                      
* │  类    名: FormationCardItem.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Module.level.Component
{
    public class FormationCardItem
    {
        //UI组件
        private Transform _root;
        private Image _icon;
        private TextMeshProUGUI _txtName;
        private Button _btnCard;
        
        private Action<int> _onClickCallback;
        public int CardIndex { get; private set; }

        public string GetCardName() => _txtName.text;
        
        public void Init(Transform rootTf, int index, Action<int> onClick)
        {
            _root = rootTf;
            _onClickCallback = onClick;
            CardIndex = index;

            _icon = _root.Find("icon").GetComponent<Image>();
            _txtName = _root.Find("Txt_name").GetComponent<TextMeshProUGUI>();
            _btnCard = _root.Find("Btn_card").GetComponent<Button>();
            
            _btnCard.onClick.AddListener(OnCardBtnClicked);
        }

        public void RefreshData(string name, Sprite icon)
        {
            Debug.Log("刷新卡牌数据，索引：" + CardIndex + ", 名称：" + name);
            _icon.sprite = icon;
            _txtName.text = name;
            //_icon.gameObject.SetActive(icon != null);
        }
        
        private void OnCardBtnClicked()
        {
            Debug.Log("点击了卡牌按钮，索引：" + CardIndex);
            _onClickCallback?.Invoke(CardIndex);
        }
    }
}