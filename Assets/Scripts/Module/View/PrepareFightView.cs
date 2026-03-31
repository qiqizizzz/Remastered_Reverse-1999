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

        protected override void OnAwake()
        {
            Find<Button>("Btn_return").onClick.AddListener(onReturnBtn);
            
            levelTargetText1 = Find<TextMeshProUGUI>("LevelDetailArea/Target/Img_content1/Txt_content1");
            levelTargetText2 = Find<TextMeshProUGUI>("LevelDetailArea/Target/Img_content2/Txt_content2");
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
    }
}