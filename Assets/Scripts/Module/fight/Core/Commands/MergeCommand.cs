/*
* ┌──────────────────────────────────┐
* │  描    述: 合成命令                      
* │  类    名: MergeCommand.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.fight.Core.Entities;

namespace Module.fight.Core.Commands
{
    public class MergeCommand : BaseCommand
    {
        public CardEntity KeptCard { get;  }
        public CardEntity DestroyedCard { get; }
        public int NewStarLevel { get; }
        
        public MergeCommand(int senderPlayerId, CardEntity keptCard, CardEntity destroyedCard, int newStarLevel) : base(senderPlayerId)
        {
            KeptCard = keptCard;
            DestroyedCard = destroyedCard;
            NewStarLevel = newStarLevel;
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