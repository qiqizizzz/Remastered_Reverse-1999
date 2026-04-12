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
        public static readonly string OpenNoticeView = "OpenNoticeView";
        public static readonly string OpenSettingView = "OpenSettingView";
        public static readonly string OpenLevelView = "OpenLevelView";
        public static readonly string OpenCharacterView = "OpenCharacterView";
        public static readonly string OpenPrepareFightView = "OpenPrepareFightView";
        public static readonly string OpenFightingView = "OpenFightingView";
        public static readonly string OpenPauseFightView = "OpenPauseFightView";
        public static readonly string LoadingScene = "LoadingScene";
        public static readonly string UpdateFriendList = "UpdateFriendList";
        public static readonly string UpdateChatHistory = "UpdateChatHistory";
        public static readonly string UpdateSearchedFriends = "UpdateSearchedFriends";
        
        // 网络事件
        public static readonly string SendPrivateMessage = "SendPrivateMessage";
        public static readonly string GetFriendList = "GetFriendList";
        public static readonly string GetSearchedFriends = "GetSearchedFriends";
        public static readonly string AddFriendRequest = "AddFriendRequest";
        public static readonly string GetChatHistory = "GetChatHistory";
        public static readonly string ReceiveNewMessage = "ReceiveNewMessage";
        public static readonly string NetWork_Disconnect = "NetWork_Disconnect";
        public static readonly string NetWork_ConnectFailed = "NetWork_ConnectFailed";
        
        
    }
}