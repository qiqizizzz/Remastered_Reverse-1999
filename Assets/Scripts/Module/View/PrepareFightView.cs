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
        private int _currentFormationCardIndex = 0;//默认从卡牌0开始
        
        public CharacterSelectItem currentCharacterSelectItem;//记录滚动列表当前选择的数据(没有点击确认键前)

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

        //提供给滚动列表查询当前编队卡牌数据的接口
        public string GetCurrentFormationCardName()
        {
            if (formationCards[_currentFormationCardIndex] != null)
                return formationCards[_currentFormationCardIndex].GetCardName();
            return String.Empty;
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
                
                formationCards[i] = item;//无论是否有卡牌都存入
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
            formationCards[index].RefreshData(name, spr);
        }
        
        private void onFormationCardBtn(int index)
        {
            setSelectFormationAreaActive(true);
            _currentFormationCardIndex = index;
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
                    if (i == _currentFormationCardIndex) return 0;
                    return 1;
                }
            }

            return 2;
        }

        //选择卡牌后回调函数
        public void OnSelectCharacterFromScroll(CharacterSelectItem item)
        {
            if (currentCharacterSelectItem != null)
            {
                currentCharacterSelectItem.setSelectedBorder(false);
            }
            
            currentCharacterSelectItem = item;
            currentCharacterSelectItem.setSelectedBorder(true);
        }
        
        private void onFormationConfirmBtn()
        {
            //情况一：没有更换卡牌,保持不变(name与此次索引的编队卡牌name相同)
            //情况二：更换了卡牌,但之前这个索引的编队为空 && 更换的卡牌不在编队中,则直接替换
            //情况三：更换了卡牌,但之前这个索引的编队为空 && 更换的卡牌在编队中,则需要将更换的卡牌所在的索引卡牌数据清空,再将更换的卡牌数据替换
            //情况四：更换了卡牌,但之前这个索引的编队不为空 && 更换的卡牌不在编队中,则直接替换
            //情况五：更换了卡牌,但之前这个索引的编队不为空 && 更换的卡牌在编队中,则需要将更换的卡牌所在的索引卡牌数据替换到之前这个索引,再将更换的卡牌数据替换
            
            selectFormationArea.gameObject.SetActive(false);
            
            if(currentCharacterSelectItem == null || currentCharacterSelectItem._characterData == null)
                return;

            string targetName = currentCharacterSelectItem.GetSelectCardName();
            string currentSlotName = formationCards[_currentFormationCardIndex].GetCardName();
            
            //情况一:没有发生任何改变
            if(targetName == currentSlotName)
                return;

            int exitIndex = -1;
            //查找新选择的角色是否已在队伍中(不包括自己)
            for (int i = 0; i < formationCards.Length; i++)
            {
                if (i != _currentFormationCardIndex && formationCards[i].GetCardName() == targetName)
                {
                    exitIndex = i;
                    break;
                }
            }

            //情况三、五
            if (exitIndex != -1)
            {
                //角色已经在该队伍中了
                updateFormationCardUI(exitIndex, currentSlotName, null);
            }
            
            //情况二、四
            updateFormationCardUI(_currentFormationCardIndex, targetName, null);
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