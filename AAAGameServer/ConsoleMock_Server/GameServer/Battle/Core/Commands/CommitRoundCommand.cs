using GameServer.Battle.Core.Entities;

namespace GameServer.Battle.Core.Commands
{
    internal class CommitRoundCommand : BaseCommand
    {
        public List<BaseCommand> RoundActions { get; }

        public CommitRoundCommand(int senderPlayerId, List<BaseCommand> roundActions) : base(senderPlayerId)
        {
            RoundActions = roundActions;
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
