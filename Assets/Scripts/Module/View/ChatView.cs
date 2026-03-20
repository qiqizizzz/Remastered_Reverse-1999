/*
* ┌──────────────────────────────────┐
* │  描    述: 好友界面                      
* │  类    名: ChatView.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common.Defines;
using MVC.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Module.View
{
    public class ChatView : BaseView
    {
        protected override void OnAwake()
        {
            Find<Button>("Btn_return").onClick.AddListener(onReturnMoreOptionBtn);
            Find<Button>("panels/panel_friend/chatArea/Btn_send").onClick.AddListener(onSendMessageBtn);
        }

        private void onReturnMoreOptionBtn()
        {
            GameApp.ViewManager.Close(ViewId);
        }

        private void onSendMessageBtn()
        {
            Debug.Log("点击了发送消息按钮");
            string targetUser = Find<TextMeshProUGUI>("panels/panel_friend/chatArea/Txt_name").text;
            string content = Find<TMP_InputField>("panels/panel_friend/chatArea/Input_field").text;

            ApplyFunc(EventDefines.SendPrivateMessage, targetUser, content);
        }
    }
}