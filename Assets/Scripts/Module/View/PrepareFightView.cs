/*
* ┌──────────────────────────────────┐
* │  描    述: 准备战斗界面                      
* │  类    名: PrepareFightView.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Data.card;
using Module.level;
using Module.level.Component;
using MVC;
using MVC.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Module.View
{
    public class PrepareFightView : BaseView
    {
        [Header("UI组件")]
        private TextMeshProUGUI levelTargetText1;
        private TextMeshProUGUI levelTargetText2;
        private Transform selectFormationArea;
        
        [Header("编队卡牌")]
        private FormationCardItem[] formationCards;
        private int _currentSelectCardIndex = 0;//默认从卡牌0开始
        private CharacterSelectItem _currentCharacterSelectItem;//记录滚动列表当前选择的数据(没有点击确认键前)

        protected override void OnAwake()
        {
            Find<Button>("Btn_return").onClick.AddListener(onReturnBtn);
            
            levelTargetText1 = Find<TextMeshProUGUI>("LevelDetailArea/Target/Img_content1/Txt_content1");
            levelTargetText2 = Find<TextMeshProUGUI>("LevelDetailArea/Target/Img_content2/Txt_content2");
            selectFormationArea = Find<Transform>("SelectFormationArea");
            
            bindFormationBtn();
            Find<Button>("SelectFormationArea/Btn_confirm").onClick.AddListener(onFormationConfirmBtn);
            Find<Button>("SelectFormationArea/Btn_cancel").onClick.AddListener(onFormationCancelBtn);
        }

        private void bindFormationBtn()
        {
            int cardCount = 4;
            formationCards = new FormationCardItem[cardCount];

            for (int i = 0; i < cardCount; i++)
            {
                Transform cardTf = Find<Transform>($"FormationArea/Card_{i}");
                
                FormationCardItem item = new FormationCardItem();
                item.Init(cardTf, i, onFormationCardBtn);
                
                if(item.GetCardName() != String.Empty)
                    formationCards[i] = item;
            }
        }

        public override void Open(params object[] args)
        {
            //args[0]为关卡id,根据关卡id显示对应的界面内容
            int levelId = (int)args[0];
            LevelData data = GameApp.ConfigManager.GetLevelData(levelId);
            if (data == null)
            {
                Debug.LogError("未找到关卡数据, id: " + levelId);
                return;
            }

            string[] desParts = data.description.Split('-');
            if (desParts.Length >= 2)
            {
                levelTargetText1.text = desParts[0];
                levelTargetText2.text = desParts[1];
            }
        }

        private void onReturnBtn()
        {
            GameApp.ViewManager.Close(ViewId);
            GameApp.ViewManager.Open(ViewType.LevelView);
        }

        #region 编队相关
        //刷新卡牌UI
        private void updateFormationCardUI(int index, string name, Sprite spr)
        {
            //TODO:考虑卡牌互换,如果name在其他卡牌上，则需要将其他卡牌的数据也进行刷新
            
            formationCards[index].RefreshData(name, spr);
        }
        
        private void onFormationCardBtn(int index)
        {
            setSelectFormationAreaActive(true);
            _currentSelectCardIndex = index;
        }
        #endregion
        
        #region 滚动列表相关

        private void setSelectFormationAreaActive(bool active)
        {
            selectFormationArea.gameObject.SetActive(active);

            if (active)
            {
                LoopVerticalScrollRect rect = Find<LoopVerticalScrollRect>("SelectFormationArea/Scroll_character");
                rect.RefreshCells();//每次打开编队选择时刷新列表
            }
        }
        
        //查询卡牌自身状态
        public int CheckCharacterState(string charName)
        {
            //0为当前选择，1为已选但非当前，2为未选择
            
            if (string.IsNullOrEmpty(charName)) return 2;

            for (int i = 0; i < formationCards.Length; i++)
            {
                if (formationCards[i] != null && formationCards[i].GetCardName() == charName)
                {
                    if (i == _currentSelectCardIndex) return 0;
                    return 1;
                }
            }

            return 2;
        }

        //选择卡牌后回调函数
        public void OnSelectCharacterFromScroll(CharacterSelectItem item)
        {
            if (_currentCharacterSelectItem != null)
            {
                _currentCharacterSelectItem.setSelectedBorder(false);
            }
            
            _currentCharacterSelectItem = item;
            _currentCharacterSelectItem.setSelectedBorder(true);
        }
        
        private void onFormationConfirmBtn()
        {
            selectFormationArea.gameObject.SetActive(false);
            //暂时不考虑卡面sprite替换
            updateFormationCardUI(_currentSelectCardIndex, _currentCharacterSelectItem._characterData.name, null);
            //TODO:保存编队信息，准备进入战斗界面
        }

        private void onFormationCancelBtn()
        {
            selectFormationArea.gameObject.SetActive(false);
            //TODO:取消编队选择，保持原有编队信息不变
        }
        #endregion
    }
}