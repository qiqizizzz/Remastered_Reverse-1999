/*
* ┌──────────────────────────────────┐
* │  描    述: 聊天数据模型                      
* │  类    名: ChatModel.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using GameProtocol;
using MVC.Model;

namespace Module.chat
{
    /// <summary>
    /// 消息发送状态
    /// </summary>
    public enum ChatMessageStatus
    {
        Sending,    // 发送中
        Success,    // 发送成功
        Failed      // 发送失败
    }
    
    /// <summary>
    /// 消息实体类
    /// </summary>
    public class ChatMessage
    {
        public string SenderName;
        public string Content;
        public long TimeStamp;
        public bool IsSelf;//是否是自己发送的消息
        public ChatMessageStatus Status;//消息发送状态
    }
    
    public class ChatModel : BaseModel
    {
        public Dictionary<string, List<ChatMessage>> ChatHistory;//聊天记录
        public Dictionary<string, int> UnreadCounts;//未读聊天数量 - string是玩家名字
        public string CurrentTargetUser;//当前聊天对象的用户名
        public List<FriendInfo> FriendList;//好友列表
        
        public const int MaxCacheCount = 100;//每个聊天对象的最大消息缓存数量
        
        public ChatModel()
        {
            ChatHistory = new Dictionary<string, List<ChatMessage>>();
            UnreadCounts = new Dictionary<string, int>();
            FriendList = new List<FriendInfo>();
            CurrentTargetUser = "";
        }
    }
}