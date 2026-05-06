/*
* ┌──────────────────────────────────┐
* │  描    述: 提交整轮行动命令                      
* │  类    名: CommitRoundCommand.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Module.fight.Core.Entities;

namespace Module.fight.Core.Commands
{
    public class CommitRoundCommand : BaseCommand
    {
        public List<BaseCommand> RoundActions { get; }
        
        public CommitRoundCommand(int senderPlayerId, List<BaseCommand> roundActions) : base(senderPlayerId)
        {
            RoundActions = roundActions;
        }

        public override bool Execute(CommandExecutionContext context)
        {
            //TODO
            return true;
        }

        public override void Undo(CommandExecutionContext context)
        {
            //TODO
        }
    }
}