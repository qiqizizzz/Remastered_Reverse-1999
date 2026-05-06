/*
* ┌──────────────────────────────────┐
* │  描    述: 命令基类                      
* │  类    名: BaseCommand.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.fight.CardMgr;
using Module.fight.Core.Entities;

namespace Module.fight.Core.Commands
{
    public abstract class BaseCommand : ICombatCommand
    {
        public int SenderPlayerId { get; }
        
        public CardSnapshot BeforeSnapshot { get; set; }

        public BaseCommand(int senderPlayerId)
        {
            SenderPlayerId = senderPlayerId;
        }

        public abstract bool Execute(CommandExecutionContext context);
        public abstract void Undo(CommandExecutionContext context);


    }
}