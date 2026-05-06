/*
* ┌──────────────────────────────────┐
* │  描    述: 战斗操作的基础接口                      
* │  类    名: ICombatCommand.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.fight.Core.Entities;

namespace Module.fight.Core.Commands
{
    public interface ICombatCommand
    {
        int SenderPlayerId { get; }

        bool Execute(CommandExecutionContext context);
        void Undo(CommandExecutionContext context);
    }
}