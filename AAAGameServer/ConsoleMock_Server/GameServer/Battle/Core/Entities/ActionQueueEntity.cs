namespace GameServer.Battle.Core.Entities
{
    public class ActionQueueEntity
    {
        public List<CardEntity> QueuedCards { get; set; }
        public int MaxQueueSize { get; set; }

        public ActionQueueEntity()
        {
            QueuedCards = new List<CardEntity>();
            MaxQueueSize = 4;
        }
    }
}
