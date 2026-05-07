namespace GameServer.Battle.Data
{
    public enum CardType { Attack, Buff, Debuff, Channel, Health, Ultimate }
    public enum EffectType { Damage, Heal, Buff, Debuff }
    public enum TargetType { Self, Enemy }

    public class CardEffect
    {
        public EffectType EffectType;
        public float Value;
        public int Round;
        public TargetType Target;
        public int TargetCount = 1;
    }

    internal class CardDataConfig
    {
        public CardType CardType;
        public int Id;
        public string Name;
        public string Description;
        public int OwnerId;
        public CardEffect[] Effects;
    }
}
