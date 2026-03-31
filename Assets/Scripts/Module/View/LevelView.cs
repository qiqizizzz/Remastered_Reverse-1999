/*
* ┌──────────────────────────────────┐
* │  描    述: 关卡界面                      
* │  类    名: LevelView.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common.Defines;
using MVC;
using MVC.View;
using UnityEngine;
using UnityEngine.UI;

namespace Module.View
{
    public class LevelView : BaseView
    {
        protected override void OnAwake()
        {
            Find<Button>("Btn_return").onClick.AddListener(onReturnBtn);
            bindLevelBtn();
        }

        private void bindLevelBtn()
        {
            Transform tf = Find<Transform>("panels/Panel_Level");
            
            for(int i = 0;i < tf.childCount;i++)
            {
                int levelId = i + 1;
                Button btn = tf.GetChild(i).GetComponent<Button>();
                btn.onClick.AddListener(() =>
                {
                    ApplyFunc(EventDefines.OpenPrepareFightView, levelId);
                });
            }
        }
        
        private void onReturnBtn()
        {
            GameApp.ViewManager.Close(ViewId);
            GameApp.ViewManager.Open(ViewType.GameView);
        }
    }
}