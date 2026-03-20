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
        private GameObject _currentChat;//当前聊天对象
        
        [Header("UI组件")]
        private TMP_InputField _inputField;
        private ScrollRect scrollRect;
        private Transform _chatPanel;
        
        protected override void OnAwake()
        {
            _inputField = Find<TMP_InputField>("panels/panel_friend/chatArea/Input_field");
            scrollRect = Find<ScrollRect>("panels/panel_friend/chatArea/Scroll_chat");
            _chatPanel = Find<Transform>("panels/panel_friend");
            
            Find<Button>("Btn_return").onClick.AddListener(onReturnMoreOptionBtn);
            Find<Button>("panels/panel_friend/chatArea/Btn_send").onClick.AddListener(onSendMessageBtn);
        }

        protected override void OnStart()
        {
            //Todo: 从数据库中读取好友列表, 生成好友列表UI, 好友按钮需要绑定相应的 点击事件
        }

        private void onReturnMoreOptionBtn()
        {
            GameApp.ViewManager.Close(ViewId);
        }

        private void onFriendBtn(string friendName)
        {
            Debug.Log("点击了好友按钮，好友名字是：" + friendName);
        }

        private void onSendMessageBtn()
        {
            string content = _inputField.text;
            string targetUser = Find<TextMeshProUGUI>("panels/panel_friend/chatArea/Txt_name").text;
            Transform contentParent = Find<Transform>("panels/panel_friend/chatArea/Scroll_chat/Viewport/Content");

            if (content == string.Empty)
            {
                Debug.Log("输入不能为空");
                ResManager.InstantiateFromPoolAsync(AddressDefines.UI_Small_TipBox, (go) =>
                {
                    if (go != null)
                    {
                        go.GetComponentInChildren<TextMeshProUGUI>().text = "输入不能为空";
                        
                        //TODO: 动画效果 - 协程

                        ResManager.ReleaseToPool(AddressDefines.UI_Small_TipBox, go);//动画结束后释放回对象池
                    }
                }, _chatPanel);
                return;
            }
            
            ResManager.InstantiateFromPoolAsync(AddressDefines.UI_Small_chatBox_me, (go) =>
            {
                if (go != null)
                {
                    go.GetComponent<ChatBubble>().SetMessage(content);
                    _inputField.text = string.Empty;
                    StartCoroutine(scrollToDown());
                }
            }, contentParent);
            
            //保存至数据库
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