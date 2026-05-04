/*
* ┌────────────────────────────────────────────┐
* │  描    述: 编队界面选择角色后卡牌仓库界面的角色卡牌                      
* │  类    名: CharacterSelectItem.cs       
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────┘
*/

using System;
using Data.card;
using Module.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Module.level.Component
{
    public class CharacterSelectItem : MonoBehaviour
    {
        private int startIndex = 1001;
        public CharacterDataSO _characterData;

        [Header("UI组件")] 
        private PrepareFightView _prepareFightView;
        private TextMeshProUGUI _nameTxt;
        private Image _img_current;
        private Image _img_selected;
        private Image _img_selectedBorder;
        
        private Button _btn_select;
        
        private void Awake()
        {
            _prepareFightView = GetComponentInParent<PrepareFightView>();
            _nameTxt = GetComponentInChildren<TextMeshProUGUI>();
            _img_current = transform.Find("Img_current").GetComponent<Image>();
            _img_selected = transform.Find("Img_selected").GetComponent<Image>();
            _img_selectedBorder = transform.Find("Img_selectedBorder").GetComponent<Image>();
            
            _btn_select = transform.Find("Btn_select").GetComponent<Button>();
            
            _btn_select.onClick.AddListener(onSelectBtn);
        }

        public string GetSelectCardName() => _nameTxt.text;

        private void onSelectBtn()
        {
            if(_prepareFightView != null && _characterData != null)
                _prepareFightView.OnSelectCharacterFromScroll(_characterData.Name);
        }

        public void setSelectedBorder(bool value)
        {
            _img_selectedBorder.gameObject.SetActive(value);
        }
        
        void ScrollCellIndex(int idx)
        {
            _characterData = GameApp.ConfigManager.Character.Get(startIndex + idx);
            if(_characterData == null) return;
            
            gameObject.name = "Card_" + _characterData.Id;
            _nameTxt.text = _characterData.Name;
            
            int state = _prepareFightView.CheckCharacterState(_nameTxt.text);
            _img_current.gameObject.SetActive(state == 0);
            _img_selected.gameObject.SetActive(state == 1);
            
            //判断是否是当前编队卡牌上选择的角色，是则显示选中边框
            bool isTargetSelected = _prepareFightView.targetSelectCharacterName == _nameTxt.text;
            setSelectedBorder(isTargetSelected);
        }
    }
}