/*
* ┌──────────────────────────────────┐
* │  描    述: 卡牌效果数据
* │  类    名: CardEffect.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace GameServer.Battle.Data.Config
{
    public class CardEffect
    {
        public EffectType EffectType { get; set; }
        public float Value { get; set; }
        public int Round { get; set; }
        public TargetType Target { get; set; }
        public int TargetCount { get; set; } = 1;
    }
}
