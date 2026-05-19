/*
* ┌──────────────────────────────────┐
* │  描    述: 视图类型
* │  类    名: ViewType.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace MVC
{
    public enum ViewType
    {
        MainMenuView, //主菜单界面
        GameView, //游戏主界面
        LoadingView, //加载界面
        MoreOptionsView, //更多选项界面
        ChatView, //好友界面
        TipBoxView, //提示框
        NoticeView, //提示界面
        SettingView, //设置界面
        LevelView, //关卡界面
        CharacterView, //角色界面
        PrepareFightView, //准备战斗界面
        FightingView, //战斗界面
        PauseFightView, //战斗暂停界面
        FightSettleView, //战斗结算界面
        MatchmakingView, //匹配界面
        RoundTipView, //回合提示界面
        BulletinView, //公告界面
    }
}