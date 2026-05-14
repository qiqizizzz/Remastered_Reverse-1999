/*
* ┌──────────────────────────────────┐
* │  描    述: 战斗运行环境，封装 BattleInstance 各子系统的共享依赖
* │  类    名: BattleEnv.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using GameServer.Battle.Core.Entities;
using GameServer.Battle.Core.EventBus;
using GameServer.Battle.Core.Serialization;
using GameServer.Battle.Core.Systems;
using GameServer.Battle.Data;

namespace GameServer.Battle.Core
{
    internal class BattleEnv
    {
        public readonly ConfigManager ConfigManager;
        public readonly CombatContext Context;
        public readonly CardCombatSystem CardSystem;
        public readonly BattleEventBuilder EventBuilder;
        public readonly BattleProtoSerializer ProtoSerializer;
        public readonly List<CombatEntity> AllEntities;
        public readonly int PlayerId;
        public readonly int MaxActionPoint;

        public BattleEnv(
            ConfigManager configManager,
            CombatContext context,
            CardCombatSystem cardSystem,
            BattleEventBuilder eventBuilder,
            BattleProtoSerializer protoSerializer,
            List<CombatEntity> allEntities,
            int playerId,
            int maxActionPoint)
        {
            ConfigManager = configManager;
            Context = context;
            CardSystem = cardSystem;
            EventBuilder = eventBuilder;
            ProtoSerializer = protoSerializer;
            AllEntities = allEntities;
            PlayerId = playerId;
            MaxActionPoint = maxActionPoint;
        }
    }
}
