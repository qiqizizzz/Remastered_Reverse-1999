/*
* ┌──────────────────────────────────┐
* │  描    述: 出牌命令                      
* │  类    名: PlayCardCommand.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.fight.Core.Entities;

namespace Module.fight.Core.Commands
{
    public class PlayCardCommand : BaseCommand
    {
        public CardEntity Card { get; }
        public string TargetInstanceId { get; }
        public int OriginalHandIndex { get; }
        
        public PlayCardCommand(int senderPlayerId, CardEntity card, string targetInstanceId, int originalHandIndex) : base(senderPlayerId)
        {
            Card = card;
            TargetInstanceId = targetInstanceId;
            OriginalHandIndex = originalHandIndex;
        }

        public override bool Execute(CombatContext ctx)
        {
            
            return true;
        }

        public override void Undo(CombatContext ctx)
        {
            //TODO
        }
    }
}