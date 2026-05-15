/*
* ┌──────────────────────────────────┐
* │  描    述: 匹配界面                      
* │  类    名: MatchmakingView.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using MVC.View;
using TMPro;
using UnityEngine.UI;

namespace Module.View
{
    public class MatchmakingView : BaseView
    {
        private TextMeshProUGUI _txt;
        
        //TODO:匹配成功后需要把_txt改为匹配成功！的逻辑，然后等待几秒再进入编队界面
        
        protected override void OnAwake()
        {
            _txt = Find<TextMeshProUGUI>("txt");
            
            Find<Button>("Btn_cancel").onClick.AddListener(onCancelBtn);
        }

        private void onCancelBtn()
        {
            //TODO:取消匹配的网络请求
            
            GameApp.ViewManager.NavigateBack();//返回上一级
        }
    }
}