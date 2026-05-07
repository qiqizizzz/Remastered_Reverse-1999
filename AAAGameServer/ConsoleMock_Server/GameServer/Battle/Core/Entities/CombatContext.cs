using GameServer.Battle.Core.EventBus;
using GameServer.Battle.Data;

namespace GameServer.Battle.Core.Entities
{
    public class CombatContext
    {
        public int CurrentRound { get; set; }

        public int SequenceCardInstanceId { get; set; }

        public ActionQueueEntity ActionQueue { get; set; }
        public Dictionary<string, CombatEntity> Entities { get; set; }
        public Dictionary<int, PlayerDeckEntity> PlayerDecks { get; set; }

        public ICardCatalog CardCatalog { get; set; }
        public CombatEventBus EventBus { get; set; }

        public CombatContext(ICardCatalog cardCatalog, CombatEventBus eventBus)
        {
            ActionQueue = new ActionQueueEntity();
            Entities = new Dictionary<string, CombatEntity>();
            PlayerDecks = new Dictionary<int, PlayerDeckEntity>();

            CardCatalog = cardCatalog;
            EventBus = eventBus;

            CurrentRound = 1;
            SequenceCardInstanceId = 1000;
        }
    }
}
