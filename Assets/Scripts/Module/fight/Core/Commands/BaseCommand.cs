/*
* ┌──────────────────────────────────┐
* │  描    述: 命令基类                      
* │  类    名: BaseCommand.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.fight.Core.Models;

namespace Module.fight.Core.Commands
{
    public class BaseCommand : ICombatCommand
    {
        public int SenderPlayerId { get; }
        public bool Execute(CombatContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}