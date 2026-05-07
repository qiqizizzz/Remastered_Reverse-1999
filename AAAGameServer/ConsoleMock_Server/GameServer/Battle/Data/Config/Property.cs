/*
* ┌──────────────────────────────────┐
* │  描    述: 角色属性数据
* │  类    名: Property.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace GameServer.Battle.Data.Config
{
    public class Property
    {
        public float Hp { get; set; }
        public float Attack { get; set; }
        public float Defense { get; set; }
        public float CritRate { get; set; }
        public float CritDamage { get; set; }
    }
}
