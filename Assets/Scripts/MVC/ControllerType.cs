/*
* ┌──────────────────────────────────┐
* │  描    述: 控制器类型
* │  类    名: ControllerType.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace MVC
{
    public enum ControllerType
    {
        GameUI,
        Loading,
        Game,  //游戏
        Chat,  //聊天
        Level, //关卡
        CultivationController, //角色
        Inventory, //背包
        Fight, //战斗
    }
}