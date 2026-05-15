/*
* ┌──────────────────────────────────┐
* │  描    述: CombatEntity / CardEntity 的 Protobuf 序列化工具
* │  类    名: BattleProtoSerializer.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using GameProtocol;
using GameServer.Battle.Core.Entities;
using GameServer.Battle.Data;

namespace GameServer.Battle.Core.Serialization
{
    internal class BattleProtoSerializer
    {
        private readonly ConfigManager _configManager;
        private const int MAX_ACTION_POINT = 5;

        public BattleProtoSerializer(ConfigManager configManager)
        {
            _configManager = configManager;
        }

        public CombatEntityInfo ToProtoEntity(CombatEntity entity, int viewerPlayerId = 1)
        {
            var charConfig = _configManager.GetCharacter(entity.ConfigId);
            int maxHp = charConfig != null ? (int)charConfig.Property.Hp : 0;

            return new CombatEntityInfo
            {
                InstanceId = entity.InstanceId,
                ConfigId = entity.ConfigId,
                IsPlayerSide = entity.OwnerPlayerId == viewerPlayerId,
                CurrentHp = (int)entity.CurrentHp,
                MaxHp = maxHp,
                ActionPoint = entity.ActionPoint,
                MaxActionPoint = MAX_ACTION_POINT
            };
        }

        public static CardEntityInfo ToProtoCard(CardEntity card)
        {
            return new CardEntityInfo
            {
                InstanceId = card.InstanceId,
                ConfigId = card.ConfigId,
                StarLevel = card.StarLevel
            };
        }
    }
}
