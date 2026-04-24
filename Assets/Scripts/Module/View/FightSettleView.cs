/*
* ┌──────────────────────────────────┐
* │  描    述: 战斗结算界面                      
* │  类    名: FightSettleView.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common.Defines;
using Module.Loading;
using MVC;
using MVC.Extensions;
using MVC.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Module.View
{
    public class FightSettleView : BaseView
    {
        [Header("UI组件")]
        private TextMeshProUGUI _titleTxt;
        private Transform _failDetail;
        private Transform _winDetail;

        protected override void OnAwake()
        {
            _titleTxt = Find<TextMeshProUGUI>("Title/Txt");
            _failDetail = Find<Transform>("Content/FailDetail");
            _winDetail = Find<Transform>("Content/WinDetail");
        }

        protected override void OnStart()
        {
            Find<Button>("Btn_return").onClick.AddListener(onReturnBtn);
        }

        public override void Open(params object[] args)
        {
            bool isWin = (bool)args[0];

            if (isWin)
            {
                _titleTxt.text = "战斗胜利";
                _winDetail.gameObject.SetActive(true);
                _failDetail.gameObject.SetActive(false);
            }
            else
            {
                _titleTxt.text = "战斗失败";
                _failDetail.gameObject.SetActive(true);
                _winDetail.gameObject.SetActive(false);
            }
        }

        private void onReturnBtn()
        {
            ApplyFunc(EventDefines.ExitLevel);
            
            ViewExtensions.LoadScene(this,SceneDefines.Game, () =>
            {
                GameApp.ViewManager.CloseAll();
                ApplyControllerFunc(ControllerType.Level, EventDefines.OpenLevelView);
            });
        }
    }
}