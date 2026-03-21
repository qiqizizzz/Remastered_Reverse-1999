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
            RegisterFunc(EventDefines.OpenChatView, onOpenChatView);
            RegisterFunc(EventDefines.SendPrivateMessage, sendPrivateMessage);
            RegisterFunc(EventDefines.GetFriendList, getFriendList);
            
            GameApp.NetworkManager.AddMessageHandler(ActionCode.ChatPrivate, onReceiveChatMsg);
            GameApp.NetworkManager.AddMessageHandler(ActionCode.FriendOperation, onReceiveFriendOperation);
        }

        public override void RemoveModuleEvent()
        {
            UnRegisterFunc(EventDefines.OpenChatView);
            UnRegisterFunc(EventDefines.SendPrivateMessage);
            UnRegisterFunc(EventDefines.GetFriendList);
            
            GameApp.NetworkManager.RemoveMessageHandler(ActionCode.ChatPrivate, onReceiveChatMsg);
            GameApp.NetworkManager.RemoveMessageHandler(ActionCode.FriendOperation, onReceiveFriendOperation);
        }

        private void onOpenChatView(System.Object[] args)
        {
            GameApp.ViewManager.Open(ViewType.ChatView);
        }

        #region 发送请求
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

        /// <summary>
        /// 获取好友列表
        /// </summary>
        /// <param name="pack"></param>
        private void getFriendList(System.Object[] args)
        {
            FriendPack friendPack = new FriendPack()
            {
                OpType = FriendOpType.GetList
            };

            MainPack mainPack = new MainPack()
            {
                RequestCode = RequestCode.Friend,
                ActionCode = ActionCode.FriendOperation,
                FriendPack = friendPack
            };
            
            GameApp.NetworkManager.Send(mainPack);
            Debug.Log("已发送获取好友列表请求");
        }
        #endregion

        #region 接收请求
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

        private void onReceiveFriendOperation(MainPack pack)
        {
            if (pack.ReturnCode == ReturnCode.Succeed)
            {
                if (pack.FriendPack != null)
                {
                    switch (pack.FriendPack.OpType)
                    {
                        case FriendOpType.GetList:
                            getFriendList(pack);
                            break;
                        case FriendOpType.AddFriend:
                            break;
                        case FriendOpType.RemoveFriend:
                            break;
                        default:
                            Debug.Log("未知的好友操作类型");
                            break;
                    }
                }
            }
            else
            {
                Debug.Log("好友操作失败");
                //TODO: 显示错误提示
            }
        }

        #region 好友相关操作

        private void getFriendList(MainPack pack)
        {
            Model.FriendList.Clear();
            foreach (var friend in pack.FriendPack.FriendList)
            {
                Model.FriendList.Add(friend);
            }
            
            Debug.Log($"成功获取到好友列表，好友数量：{Model.FriendList.Count}");
            
            ApplyFunc(EventDefines.UpdateFriendList);//通知视图更新好友列表
        }

        #endregion
        
        #endregion
        
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