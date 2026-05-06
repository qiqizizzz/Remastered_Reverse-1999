/*
* ┌──────────────────────────────────┐
* │  描    述: 移动卡牌命令                      
* │  类    名: MoveCardCommand.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.fight.Core.Entities;

namespace Module.fight.Core.Commands
{
    public class MoveCardCommand : BaseCommand
    {
        public int FromIndex { get; }
        public int ToIndex { get; }
        
        public MoveCardCommand(int senderPlayerId, int fromIndex, int toIndex) : base(senderPlayerId)
        {
            FromIndex = fromIndex;
            ToIndex = toIndex;
        }

        public override bool Execute(CombatContext context)
        {
            //TODO
            return true;
        }

        public override void Undo(CombatContext context)
        {
            //TODO
        }
    }
}