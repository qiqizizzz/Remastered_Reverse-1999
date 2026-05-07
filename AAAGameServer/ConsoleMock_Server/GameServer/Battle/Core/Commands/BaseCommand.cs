using GameServer.Battle.Core.Entities;
using GameServer.Battle.Data;

namespace GameServer.Battle.Core.Commands
{
    internal abstract class BaseCommand : ICombatCommand
    {
        public int SenderPlayerId { get; }

        public CardSnapshot BeforeSnapshot { get; set; }

        public BaseCommand(int senderPlayerId)
        {
            SenderPlayerId = senderPlayerId;
        }

        public abstract bool Execute(CombatContext context);
        public abstract void Undo(CombatContext context);
    }
}
