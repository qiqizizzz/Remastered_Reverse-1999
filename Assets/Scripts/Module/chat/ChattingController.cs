/*
* ┌──────────────────────────────────┐
* │  描    述: 聊天控制器                      
* │  类    名: ChatController.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using System.Linq;
using Common.Defines;
using GameProtocol;
using Module.View;
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
            RegisterFunc(EventDefines.GetChatHistory, getChatHistory);
            RegisterFunc(EventDefines.GetSearchedFriends, getSearchedFriends);
            RegisterFunc(EventDefines.AddFriendRequest, addFriendRequest);
            
            GameApp.NetworkManager.AddMessageHandler(ActionCode.ChatPrivate, onReceiveChatMsg);
            GameApp.NetworkManager.AddMessageHandler(ActionCode.FriendOperation, onReceiveFriendOperation);
            GameApp.NetworkManager.AddMessageHandler(ActionCode.GetChatHistory, onReceiveChatHistory);
        }

        public override void RemoveModuleEvent()
        {
            UnRegisterFunc(EventDefines.OpenChatView);
            UnRegisterFunc(EventDefines.SendPrivateMessage);
            UnRegisterFunc(EventDefines.GetFriendList);
            UnRegisterFunc(EventDefines.GetChatHistory);
            UnRegisterFunc(EventDefines.GetSearchedFriends);
            UnRegisterFunc(EventDefines.AddFriendRequest);
            
            GameApp.NetworkManager.RemoveMessageHandler(ActionCode.ChatPrivate, onReceiveChatMsg);
            GameApp.NetworkManager.RemoveMessageHandler(ActionCode.FriendOperation, onReceiveFriendOperation);
            GameApp.NetworkManager.RemoveMessageHandler(ActionCode.GetChatHistory, onReceiveChatHistory);
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
        }
        
        private void getChatHistory(System.Object[] args)
        {
            string targetUser = args[0] as string;

            MainPack pack = new MainPack
            {
                RequestCode = RequestCode.User,
                ActionCode = ActionCode.GetChatHistory,
                ChatHistoryPack = new ChatHistoryPack { TargetUser = targetUser }
            };
            GameApp.NetworkManager.Send(pack);
        }

        private void getSearchedFriends(System.Object[] args)
        {
            //args[0]代表搜索关键词
            string targetUser = args[0] as string;
            if(string.IsNullOrEmpty(targetUser)) return;

            MainPack  pack = new MainPack()
            {
                ActionCode = ActionCode.FriendOperation,
                RequestCode = RequestCode.Friend,
                FriendPack = new FriendPack()
                {
                    OpType = FriendOpType.SearchUser,
                    TargetUser = targetUser
                }
            };
            
            GameApp.NetworkManager.Send(pack);
        }

        private void addFriendRequest(System.Object[] args)
        {
            //args[0]代表要添加的好友名字
            string targetUser = args[0] as string;
            if(string.IsNullOrEmpty(targetUser)) return;
            
            MainPack  pack = new MainPack()
            {
                ActionCode = ActionCode.FriendOperation,
                RequestCode = RequestCode.Friend,
                FriendPack = new FriendPack()
                {
                    OpType = FriendOpType.AddFriend,
                    TargetUser = targetUser
                }
            };
                
            GameApp.NetworkManager.Send(pack);
        }
        #endregion

        #region 接收请求
        private void onReceiveChatMsg(MainPack pack)
        {
            Debug.Log($"收到聊天消息的服务器响应，返回码：{pack.ReturnCode}");
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
                    Debug.Log("收到来自 " + sender + " 的消息: " + content);

                    ApplyFunc(EventDefines.ReceiveNewMessage, sender, msg);
                }
                else
                {
                    Debug.Log($"发送成功");
                    //TODO：更新消息状态为成功，并且刷新界面显示
                }
            }
            else
            {
                Debug.Log($"发送失败,请检查网络连接");
                GameApp.ViewManager.Open(ViewType.TipBoxView, TipBoxType.chat, "发送失败");
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
                            getAddFriendResult(pack);
                            break;
                        case FriendOpType.RemoveFriend:
                            break;
                        case FriendOpType.SearchUser:
                            getSearchedUser(pack);
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
        
        private void onReceiveChatHistory(MainPack pack)
        {
            if (pack.ReturnCode == ReturnCode.Succeed && pack.ChatHistoryPack != null)
            {
                string targetUser = pack.ChatHistoryPack.TargetUser;
                string myName = GameApp.GameDataManager.PlayerName;

                //清空旧缓存
                if (Model.ChatHistory.ContainsKey(targetUser))
                {
                    Model.ChatHistory[targetUser].Clear();
                }

                foreach (var chatMsg in pack.ChatHistoryPack.ChatList)
                {
                    bool isSelf = (chatMsg.FromUser == myName); // 判断这条消息是不是自己发的

                    ChatMessage msg = new ChatMessage()
                    {
                        SenderName = chatMsg.FromUser,
                        Content = chatMsg.Content,
                        TimeStamp = chatMsg.Timestamp,
                        IsSelf = isSelf,
                        Status = ChatMessageStatus.Success
                    };
            
                    saveToModel(targetUser, msg);
                }

                Debug.Log($"成功拉取与 {targetUser} 的历史记录，共 {pack.ChatHistoryPack.ChatList.Count} 条");

                ApplyFunc(EventDefines.UpdateChatHistory, targetUser);
            }
            else
            {
                Debug.Log("没有获取到历史记录");
            }
        }
        
        #region 好友相关操作

        private void getFriendList(MainPack pack)
        {
            Model.FriendList.Clear();
            foreach (var friend in pack.FriendPack.FriendList)
            {
                Model.FriendList.Add(friend);
                Debug.Log(friend);
            }
            
            Debug.Log($"成功获取到好友列表，好友数量：{Model.FriendList.Count}");
            
            ApplyFunc(EventDefines.UpdateFriendList);//通知视图更新好友列表
        }

        private void getSearchedUser(MainPack pack)
        {
            List<FriendInfo> searchedUser = new List<FriendInfo>(pack.FriendPack.FriendList);
            List<FriendInfo> finalSearched = new List<FriendInfo>(searchedUser).Except(Model.FriendList).ToList();
            
            Debug.Log($"成功获取到搜索列表，搜索数量：{finalSearched.Count}");
            
            ApplyFunc(EventDefines.UpdateSearchedFriends, finalSearched);//通知视图更新搜索结果
        }

        private void getAddFriendResult(MainPack pack)
        {
            if (pack.ReturnCode == ReturnCode.Succeed)
            {
                Debug.Log("好友添加成功");
            }
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