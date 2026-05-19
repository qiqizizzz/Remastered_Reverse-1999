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
        public const string OpenGameView = "OpenGameView";
        public const string OpenMainMenuView = "OpenMainMenuView";
        public const string OpenMoreOptionsView = "OpenMoreOptionsView";
        public const string OpenChatView = "OpenChatView";
        public const string OpenTipBoxView = "OpenTipBoxView";
        public const string OpenNoticeView = "OpenNoticeView";
        public const string OpenSettingView = "OpenSettingView";
        public const string OpenLevelView = "OpenLevelView";
        public const string OpenCharacterView = "OpenCharacterView";
        public const string OpenPrepareFightView = "OpenPrepareFightView";
        public const string OpenFightingView = "OpenFightingView";
        public const string OpenPauseFightView = "OpenPauseFightView";
        public const string OpenFightSettleView = "OpenFightSettleView";
        public const string OpenBulletinView = "OpenBulletinView";
        public const string LoadingScene = "LoadingScene";
        public const string UpdateFriendList = "UpdateFriendList";
        public const string UpdateChatHistory = "UpdateChatHistory";
        public const string UpdateSearchedFriends = "UpdateSearchedFriends";
        public const string UpdateHandCards = "UpdateHandCards";
        public const string FightingViewReady = "FightingViewReady";
        public const string ExitLevel = "ExitLevel";
        
        // 网络事件
        public const string SendPrivateMessage = "SendPrivateMessage";
        public const string GetFriendList = "GetFriendList";
        public const string GetSearchedFriends = "GetSearchedFriends";
        public const string AddFriendRequest = "AddFriendRequest";
        public const string GetChatHistory = "GetChatHistory";
        public const string ReceiveNewMessage = "ReceiveNewMessage";
        public const string NetWork_Disconnect = "NetWork_Disconnect";
        public const string NetWork_ConnectFailed = "NetWork_ConnectFailed";
        
        //战斗相关事件
        public const string OnPlayerTurnStart = "OnPlayerTurnStart";
        public const string OnPlayerTurnOutput = "OnPlayerTurnOutput";
        public const string OnSelectEnemyTarget = "OnSelectEnemyTarget";
        public const string OnCardExecuteUI = "OnCardExecuteUI";
        public const string OnMoveActionExecute = "OnMoveActionExecute";
        public const string OnCharacterDie = "OnCharacterDie";
        public const string OnRemoveDiedCharacterCard = "OnRemoveDiedCharacterCard";
        public const string OnHandCardChanged = "OnHandCardChanged";
        
        //战斗卡牌相关事件
        public const string OnHandCardMerged = "OnHandCardMerged";
        
        // 网络战斗事件（服务端推送）
        public const string OnBattleServerResponse = "OnBattleServerResponse";
        public const string OnBattleTurnStart = "OnBattleTurnStart";
        public const string OnBattleTurnEnd = "OnBattleTurnEnd";
        public const string OnPvpMatchSuccess = "OnPvpMatchSuccess";
        public const string OnPvpMatchFailed = "OnPvpMatchFailed";
        public const string OnPvpTeamWaiting = "OnPvpTeamWaiting";
        public const string OnPvpBattleStart = "OnPvpBattleStart";
    }
}