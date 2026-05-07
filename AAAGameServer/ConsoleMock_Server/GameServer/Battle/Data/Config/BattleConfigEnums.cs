/*
* ┌──────────────────────────────────┐
* │  描    述: 战斗配置相关枚举定义
* │  类    名: BattleConfigEnums.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace GameServer.Battle.Data.Config
{
    public enum CardType
    {
        Attack,
        Buff,
        Debuff,
        Channel,
        Health,
        Ultimate
    }

    public enum EffectType
    {
        Damage,
        Heal,
        Buff,
        Debuff
    }

    public enum TargetType
    {
        Self,
        Enemy
    }

    public enum CharacterType
    {
        Hero,
        Monster
    }

    public enum InspirationType
    {
        Beast,
        Wood,
        Star,
        Rock
    }
}
