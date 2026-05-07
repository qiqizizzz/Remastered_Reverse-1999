/*
* ┌──────────────────────────────────┐
* │  描    述: 关卡配置数据（对应JSON导出的levels）
* │  类    名: LevelDataConfig.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;

namespace GameServer.Battle.Data.Config
{
    public class LevelDataConfig
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<MonsterSpawnData> MonsterSpawns { get; set; }
    }
}
