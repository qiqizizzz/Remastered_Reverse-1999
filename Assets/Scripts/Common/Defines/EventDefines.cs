/*
* ┌──────────────────────────────────┐
* │  描    述: 事件定义类                      
* │  类    名: EventDefines.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Common.Defines
{
    public static class EventDefines
    {
        // UI事件
        public static readonly string OpenGameView = "OpenGameView";
        public static readonly string OpenMainMenuView = "OpenMainMenuView";
        public static readonly string OpenMoreOptionsView = "OpenMoreOptionsView";
        public static readonly string OpenChatView = "OpenChatViewView";
        public static readonly string OpenTipBoxView = "OpenTipBoxView";
        public static readonly string LoadingScene = "LoadingScene";
        public static readonly string UpdateFriendList = "UpdateFriendList";
        public static readonly string UpdateChatHistory = "UpdateChatHistory";
        
        // 网络事件
        public static readonly string SendPrivateMessage = "SendPrivateMessage";
        public static readonly string GetFriendList = "GetFriendList";
        public static readonly string GetChatHistory = "GetChatHistory";
        public static readonly string ReceiveNewMessage = "ReceiveNewMessage";
    }
}