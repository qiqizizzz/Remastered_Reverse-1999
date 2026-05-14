/*
* ┌──────────────────────────────────┐
* │  描    述: 关卡怪物生成数据
* │  类    名: MonsterSpawnData.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace GameServer.Battle.Data.Config
{
    public class MonsterSpawnData
    {
        [System.Text.Json.Serialization.JsonPropertyName("monsterId")]
        public int MonsterId { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("count")]
        public int Count { get; set; }
    }
}
