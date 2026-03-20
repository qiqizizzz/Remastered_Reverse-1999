/*
* ┌──────────────────────────────────┐
* │  描    述: 好友界面                      
* │  类    名: ChatView.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections;
using Common;
using Common.Defines;
using Module.chat;
using MVC.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Module.View
{
    public class ChatView : BaseView
    {
        private TMP_InputField _inputField;
        private ScrollRect scrollRect;
        
        protected override void OnAwake()
        {
            _inputField = Find<TMP_InputField>("panels/panel_friend/chatArea/Input_field");
            scrollRect = Find<ScrollRect>("panels/panel_friend/chatArea/Scroll_chat");
            
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
            string content = _inputField.text;
            string targetUser = Find<TextMeshProUGUI>("panels/panel_friend/chatArea/Txt_name").text;
            Transform parent = Find<Transform>("panels/panel_friend/chatArea/Scroll_chat/Viewport/Content");

            if (content == string.Empty)
            {
                Debug.Log("输入不能为空");
                return;
                //Todo:提示输入不能为空
            }
            
            //TODO：ResManager生成气泡,并且把消息内容传给气泡
            ResManager.InstantiateFromPoolAsync(AddressDefines.UI_Small_chatBox_me, (go) =>
            {
                if (go != null)
                {
                    go.GetComponent<ChatBubble>().SetMessage(content);
                    _inputField.text = string.Empty;
                    StartCoroutine(scrollToDown());
                }
            }, parent);

            Debug.Log("成功生成气泡");
            
            //ApplyFunc(EventDefines.SendPrivateMessage, targetUser, content);
        }

        private IEnumerator scrollToDown()
        {
            yield return new WaitForEndOfFrame();
            
            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();//强制刷新
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }
    }
}