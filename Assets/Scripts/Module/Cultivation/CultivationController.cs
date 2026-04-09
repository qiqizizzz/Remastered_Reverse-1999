/*
* ┌────────────────────────────────────────┐
* │  描    述: 角色控制器(管理角色数据、养成等)                      
* │  类    名: CharacterController.cs       
* │  创    建: By qiqizizzz
* └────────────────────────────────────────┘
*/

using Common.Defines;
using MVC;
using MVC.Controller;

namespace Module.Cultivation
{
    public class CultivationController : BaseController
    {
        public CultivationController() : base()
        {
            GameApp.ViewManager.Register(ViewType.CharacterView, new ViewInfo()
            {
                controller = this,
                parentTf = GameApp.ViewManager.canvasTf,
                PrefabName = AddressDefines.UI_CharacterView
            });
            
            InitModuleEvent();
        }

        public override void InitModuleEvent()
        {
            RegisterFunc(EventDefines.OpenCharacterView, onOpenCharacterView);
        }

        private void onOpenCharacterView(params object[] args)
        {
            GameApp.ViewManager.Open(ViewType.CharacterView, args);
        }
        
    }
}