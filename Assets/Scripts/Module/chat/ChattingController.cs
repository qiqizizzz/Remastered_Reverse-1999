/*
* ┌──────────────────────────────────┐
* │  描    述: 聊天控制器                      
* │  类    名: ChatController.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Common.Defines;
using GameProtocol;
using MVC;
using MVC.Controller;
using UnityEngine;

namespace Module.chat
{
    public class ChattingController : BaseController
    {
        public ChatModel Model { get; private set; }
        
        public ChattingController() : base()
        {
            Model = new ChatModel();
            
            //注册视图
            GameApp.ViewManager.Register(ViewType.ChatView, new ViewInfo()
            {
                PrefabName = AddressDefines.UI_FriendsView,
                parentTf = GameApp.ViewManager.canvasTf,
                controller = this,
                Sorting_Order = 2
            });

            InitModuleEvent();
        }

        public override void InitModuleEvent()
        {
            RegisterFunc(EventDefines.SendPrivateMessage, sendPrivateMessage);
            RegisterFunc(EventDefines.OpenChatView, onOpenChatView);
            GameApp.NetworkManager.AddMessageHandler(ActionCode.ChatPrivate, onReceiveChatMsg);
        }

        public override void RemoveModuleEvent()
        {
            UnRegisterFunc(EventDefines.SendPrivateMessage);
            UnRegisterFunc(EventDefines.OpenChatView);
            GameApp.NetworkManager.RemoveMessageHandler(ActionCode.ChatPrivate, onReceiveChatMsg);
        }

        private void onOpenChatView(System.Object[] args)
        {
            GameApp.ViewManager.Open(ViewType.ChatView);
        }

        /// <summary>
        /// 私聊发送消息
        /// </summary>
        /// <param name="args">args[0]代表targetUser,args[1]代表content</param>
        private void sendPrivateMessage(System.Object[] args)
        {
            string targetUser = args.Length > 0 ? args[0] as string : null;
            string content = args.Length > 1 ? args[1] as string : null;
            
            if(string.IsNullOrEmpty(targetUser) || string.IsNullOrEmpty(content)) return;

            long timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            //定义本地存储
            ChatMessage chatMsg = new ChatMessage()
            {
                SenderName = GameApp.GameDataManager.PlayerName,
                Content = content,
                TimeStamp = timestamp,
                IsSelf = true,
                Status = ChatMessageStatus.Sending
            };
            
            saveToModel(targetUser, chatMsg);
            
            //定义服务端发送包
            ChatPack chatPack = new ChatPack()
            {
                ChatType = 0, //纯文本消息
                Content = content,
                Timestamp = timestamp,
                ToUser = targetUser,
            };

            MainPack mainPack = new MainPack()
            {
                ActionCode = ActionCode.ChatPrivate,
                RequestCode = RequestCode.User,
                ChatPack = chatPack
            };
            
            GameApp.NetworkManager.Send(mainPack);
        }

        private void onReceiveChatMsg(MainPack pack)
        {
            if (pack.ReturnCode == ReturnCode.Succeed)
            {
                //如果是别人发来的消息
                if (pack.ChatPack != null && !string.IsNullOrEmpty(pack.ChatPack.FromUser))
                {
                    string sender = pack.ChatPack.FromUser;
                    string content = pack.ChatPack.Content;
                    long timestamp = pack.ChatPack.Timestamp;

                    ChatMessage msg = new ChatMessage()
                    {
                        SenderName = sender,
                        Content = content,
                        TimeStamp = timestamp,
                        IsSelf = false, //别人发的
                        Status = ChatMessageStatus.Success
                    };
                    
                    saveToModel(sender, msg);
                    
                    //TODO：刷新气泡,显示收到的消息
                }
                else
                {
                    Debug.Log($"发送成功");
                    //TODO：更新消息状态为成功，并且刷新界面显示
                }
            }
            else
            {
                Debug.LogError($"发送失败,请检查网络连接");
                //TODO：更新消息状态为失败，并且刷新界面显示
            }
        }
        
        private void saveToModel(string targetUser, ChatMessage msg)
        {
            if (!Model.ChatHistory.ContainsKey(targetUser))
            {
                Model.ChatHistory[targetUser] = new List<ChatMessage>();
            }
            
            var history = Model.ChatHistory[targetUser];
            
            history.Add(msg);
            
            //若消息超过最大缓存容量
            if (history.Count > ChatModel.MaxCacheCount)
            {
                history.RemoveAt(0);//移除最早的一条消息
            }
        }
        
    }
}