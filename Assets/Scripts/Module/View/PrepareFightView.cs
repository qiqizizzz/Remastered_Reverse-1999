/*
* ┌──────────────────────────────────┐
* │  描    述: 准备战斗界面                      
* │  类    名: PrepareFightView.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.level;
using MVC.View;

namespace Module.View
{
    public class PrepareFightView : BaseView
    {
        public override void Open(params object[] args)
        {
            //args[0]为关卡id,根据关卡id显示对应的界面内容
            int levelId = (int)args[0];
            //LevelData data = GameApp.ConfigManager.Get
            
        }
    }
}