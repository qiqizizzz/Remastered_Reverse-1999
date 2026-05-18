/*
* ┌──────────────────────────────────┐
* │  描    述: 红点管理器                      
* │  类    名: RedDotManager.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common;
using Module.RedDot.Component;

namespace Module.RedDot
{
    public class RedDotManager
    {
        private RedDotNode _root;//虚拟根节点
        private const char _separator = '/';

        public RedDotManager()
        {
            _root = new RedDotNode("Root");
            
            QLog.Info("RedDotManager initialized.");
        }

        public void Destroy()
        {
            _root = null;
        }
        
        
    }
}