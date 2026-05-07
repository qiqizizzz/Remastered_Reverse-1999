/*
* ┌──────────────────────────────────┐
* │  描    述: 角色配置数据（对应JSON导出的heroes/enemies）
* │  类    名: CharacterDataConfig.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;

namespace GameServer.Battle.Data.Config
{
    public class CharacterDataConfig
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string En_Name { get; set; }
        public CharacterType CharacterType { get; set; }
        public InspirationType InspirationType { get; set; }
        public Property Property { get; set; }
        public List<int> Cards { get; set; }
    }
}
