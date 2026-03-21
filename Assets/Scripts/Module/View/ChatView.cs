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
        private Button _currentChat;//当前聊天对象
        private const int friendMaxCount = 50;//好友上限
        
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
            Controller.RegisterFunc(EventDefines.UpdateChatHistory, onUpdateChatHistory);
            
            ApplyFunc(EventDefines.GetFriendList);//发送获取好友列表请求
        }

        protected override void OnDestroy()
        {
            Controller.UnRegisterFunc(EventDefines.UpdateFriendList);
            Controller.UnRegisterFunc(EventDefines.GetChatHistory);
        }

        private void onReturnMoreOptionBtn()
        {
            GameApp.ViewManager.Close(ViewId);
        }

        private void onFriendBtn(string friendName, GameObject btnObj)
        {
            Find<TextMeshProUGUI>("panels/panel_friend/chatArea/Txt_name").text = friendName;
            
            //取消之前的选中状态
            if(_currentChat != null)
                _currentChat.transform.Find("Imgs_bg/Img_selected").gameObject.SetActive(false);
            
            btnObj.transform.Find("Imgs_bg/Img_selected").gameObject.SetActive(true);//设置当前按钮的选中状态
            
            //如果点击的好友已经是当前聊天对象，则不重复加载聊天记录
            if(_currentChat == btnObj.GetComponent<Button>())
                return;
            
            _currentChat = btnObj.GetComponent<Button>();
            ApplyFunc(EventDefines.GetChatHistory, friendName);
            
            //TODO: 加载与该好友的聊天记录，并刷新聊天界面
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
            ApplyFunc(EventDefines.SendPrivateMessage, targetUser, content);
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
            
            Transform contentParent = _friendScrollRect.content; 
            List<FriendInfo> friends = (Controller as ChattingController)?.Model.FriendList;

            if (friends == null)
            {
                Debug.LogError("Model.FriendList 是空的！请检查 Controller 里是否正确赋值了！");
                return;
            }

            if (friends.Count == 0)
            {
                //TODO: 显示没有好友的提示
            }
            
            Find<TextMeshProUGUI>("panels/panel_friend/chatArea/Txt_name").text = friends[0].Username;
            
            //回收先前的好友列表UI
            foreach (Transform child in contentParent)
            {
                ResManager.ReleaseToPool(AddressDefines.UI_Small_Btn_friTemp, child.gameObject);
            }
            
            //更新好友数量
            Find<TextMeshProUGUI>("panels/panel_friend/Txt_num").text = $"{friends.Count}/{friendMaxCount}";
            
            foreach (var item in friends)
            {
                string targetName = item.Username;
                
                ResManager.InstantiateFromPoolAsync(AddressDefines.UI_Small_Btn_friTemp,(go =>
                {
                    if (go != null)
                    {
                        go.transform.Find("Txt_name").GetComponent<TextMeshProUGUI>().text = targetName;
                        
                        //获取状态节点
                        TextMeshProUGUI txtTime = go.transform.Find("Txt_loginTime").GetComponent<TextMeshProUGUI>();
                        if (item.IsOnline)
                            txtTime.text = "<color=green>在线</color>";
                        else
                            txtTime.text = "<color=#716860>离线</color>";
                        
                        Button btn = go.GetComponent<Button>();
                        btn.onClick.RemoveAllListeners();//先移除之前的监听，避免重复绑定
                        btn.onClick.AddListener(() => onFriendBtn(targetName ,go));
                    }
                }),contentParent);
            }
        }

        private void onUpdateChatHistory(params object[] args)
        {
            Debug.Log("更新历史记录中...");
            //args[0] 是聊天对象的名字
            string targetUser = args[0] as string;
            Transform contentParent = _chatScrollRect.content;
            List<ChatMessage> messages =
                (Controller as ChattingController)?.Model.ChatHistory.GetValueOrDefault(targetUser) ??
                new List<ChatMessage>();
            
            if(messages.Count == 0) return;
            
            //回收先前的聊天记录UI
            foreach (Transform child in contentParent)
            {
                ResManager.ReleaseToPool(AddressDefines.UI_Small_chatBox_me, child.gameObject);
                ResManager.ReleaseToPool(AddressDefines.UI_Small_chatBox_other, child.gameObject);
            }
                
            foreach (var msg in messages)
            {
                string content = msg.Content;
                    
                if (msg.IsSelf)
                {
                    ResManager.InstantiateFromPoolAsync(AddressDefines.UI_Small_chatBox_me, (go) =>
                    {
                        if (go != null)
                        {
                            go.GetComponent<ChatBubble>().SetMessage(content);
                        }
                    }, contentParent);
                }
                else
                {
                    ResManager.InstantiateFromPoolAsync(AddressDefines.UI_Small_chatBox_other, (go) =>
                    {
                        if (go != null)
                        {
                            go.GetComponent<ChatBubble>().SetMessage(content);
                        }
                    }, contentParent);
                }
            }
            
            StartCoroutine(scrollToDown());
        }
    }
}