/*
* ┌──────────────────────────────────┐
* │  描    述: 好友界面                      
* │  类    名: ChatView.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections;
using System.Collections.Generic;
using Common;
using Common.Defines;
using GameProtocol;
using Module.chat;
using MVC;
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
        private ScrollRect _chatScrollRect;
        private ScrollRect _friendScrollRect;
        private Transform _chatPanel;
        
        protected override void OnAwake()
        {
            _inputField = Find<TMP_InputField>("panels/panel_friend/chatArea/Input_field");
            _chatScrollRect = Find<ScrollRect>("panels/panel_friend/chatArea/Scroll_chat");
            _friendScrollRect = Find<ScrollRect>("panels/panel_friend/Scroll_hy");
            _chatPanel = Find<Transform>("panels/panel_friend");
            
            Find<Button>("Btn_return").onClick.AddListener(onReturnMoreOptionBtn);
            Find<Button>("panels/panel_friend/chatArea/Btn_send").onClick.AddListener(onSendMessageBtn);
            
            
        }

        protected override void OnStart()
        {
            Controller.RegisterFunc(EventDefines.UpdateFriendList, onUpdateFriendList);
            ApplyFunc(EventDefines.GetFriendList);//发送获取好友列表请求
        }

        protected override void OnDestroy()
        {
            Controller.UnRegisterFunc(EventDefines.UpdateFriendList);
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
                ApplyControllerFunc(ControllerType.GameUI, EventDefines.OpenTipBoxView, TipBoxType.chat, "输入不能为空");
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
            
            if (_chatScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();//强制刷新
                _chatScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private void onUpdateFriendList(params object[] args)
        {
            Debug.Log("更新好友列表中...");
            //Todo: 从数据库中读取好友列表, 生成好友列表UI, 好友按钮需要绑定相应的 点击事件
            //List<FriendInfo> friends = Controller.GetModel<ChatModel>().FriendList;
        }
    }
}