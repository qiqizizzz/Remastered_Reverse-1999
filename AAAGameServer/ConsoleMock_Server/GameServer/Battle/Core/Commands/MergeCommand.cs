using GameServer.Battle.Core.Entities;

namespace GameServer.Battle.Core.Commands
{
    internal class MergeCommand : BaseCommand
    {
        public CardEntity KeptCard { get; }
        public CardEntity DestroyedCard { get; }
        public int NewStarLevel { get; }

        public MergeCommand(int senderPlayerId, CardEntity keptCard, CardEntity destroyedCard, int newStarLevel) : base(senderPlayerId)
        {
            KeptCard = keptCard;
            DestroyedCard = destroyedCard;
            NewStarLevel = newStarLevel;
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
