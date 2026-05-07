using GameServer.Battle.Core.Entities;

namespace GameServer.Battle.Core.Commands
{
    internal interface ICombatCommand
    {
        int SenderPlayerId { get; }

        bool Execute(CombatContext context);
        void Undo(CombatContext context);
    }
}
