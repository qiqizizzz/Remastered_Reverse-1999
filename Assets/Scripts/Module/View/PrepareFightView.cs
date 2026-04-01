/*
* ┌──────────────────────────────────┐
* │  描    述: 准备战斗界面                      
* │  类    名: PrepareFightView.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
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
        private int currentSelectCardIndex = 0;//默认从卡牌0开始

        [Header("滚动列表")] 
        private Dictionary<string, CharacterSelectItem> characterSelectItems;
        private bool isScrollListInitialized = false;

        protected override void OnAwake()
        {
            characterSelectItems = new Dictionary<string, CharacterSelectItem>();
            
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
            formationCards[index].RefreshData(name, spr);
        }
        
        private void onFormationCardBtn(int index)
        {
            setSelectFormationAreaActive(true);
            currentSelectCardIndex = index;
            //TODO:在selectFormationArea界面显示可选的角色卡牌列表，玩家选择后替换当前编队位的卡牌信息
        }
        #endregion
        
        #region 滚动列表相关

        private void setSelectFormationAreaActive(bool active)
        {
            selectFormationArea.gameObject.SetActive(active);
            
            if (isScrollListInitialized == false && active)
            {
                LoopVerticalScrollRect scrollRect = Find<LoopVerticalScrollRect>("SelectFormationArea/Scroll_character");

                Debug.Log("初始化滚动列表，卡牌数量：" + scrollRect.content.childCount);
                
                for (int i = 0; i < scrollRect.content.childCount; i++)
                {
                    CharacterSelectItem item = scrollRect.content.GetChild(i).GetComponent<CharacterSelectItem>();
                    characterSelectItems.Add(item.GetSelectCardName(), item);
                    Debug.Log($"已加入角色卡牌到字典，名称：{item.GetSelectCardName()}");
                }

                foreach (FormationCardItem item in formationCards)
                {
                    updateScrollUI(item.GetCardName(), 1);
                }
                
                isScrollListInitialized = true;
            }
        }
        
        
        private void updateScrollUI(string name, int type)
        {
            characterSelectItems.TryGetValue(name, out CharacterSelectItem item);

            if (item == null)
            {
                Debug.Log("未找到角色卡牌，名称：" + name);
                return;
            }
            
            //0表示当前，1表示已选，2表示未选择
            switch (type)
            {
                case 0:
                    Debug.Log("设置为当前");
                    Find<Image>("Img_current", item.transform).gameObject.SetActive(true);
                    break;
                case 1:
                    Debug.Log("设置为已选择");
                    Find<Image>("Img_selected", item.transform).gameObject.SetActive(true);
                    break;
                case 2:
                    Debug.Log("设置为未选择");
                    Find<Image>("Img_current", item.transform).gameObject.SetActive(false);
                    Find<Image>("Img_selected", item.transform).gameObject.SetActive(false);
                    break;
            }
        }
        
        private void onFormationConfirmBtn()
        {
            selectFormationArea.gameObject.SetActive(false);
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