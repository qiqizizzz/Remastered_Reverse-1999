/*
* ┌──────────────────────────────────┐
* │  描    述: 准备战斗界面                      
* │  类    名: PrepareFightView.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.level;
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
        private Transform card_1;
        private Transform card_2;
        private Transform card_3;
        private Transform card_4;
        

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
            card_1 = Find<Transform>("FormationArea/Card_1").transform;
            card_2 = Find<Transform>("FormationArea/Card_2").transform;
            card_3 = Find<Transform>("FormationArea/Card_3").transform;
            card_4 = Find<Transform>("FormationArea/Card_4").transform;
            
            Find<Button>("FormationArea/Card_1/Btn_card").onClick.AddListener(() =>
            {
                onSelectCardBtn(card_1);
            });
            Find<Button>("FormationArea/Card_2/Btn_card").onClick.AddListener(() =>
            {
                onSelectCardBtn(card_2);
            });
            Find<Button>("FormationArea/Card_3/Btn_card").onClick.AddListener(() =>
            {
                onSelectCardBtn(card_3);
            });
            Find<Button>("FormationArea/Card_4/Btn_card").onClick.AddListener(() =>
            {
                onSelectCardBtn(card_4);
            });
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

        private void onSelectCardBtn(Transform cardTf)
        {
            Debug.Log($"点击了{cardTf.name}");
            selectFormationArea.gameObject.SetActive(true);
            //TODO:在selectFormationArea界面显示可选的角色卡牌列表，玩家选择后替换当前编队位的卡牌信息
        }

        private void onReturnBtn()
        {
            GameApp.ViewManager.Close(ViewId);
            GameApp.ViewManager.Open(ViewType.LevelView);
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
    }
}