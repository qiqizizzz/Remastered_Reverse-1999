/*
* ┌──────────────────────────────────┐
* │  描    述: 关卡初始化数据                      
* │  类    名: LevelInitData.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Data.card;
using Module.level;

namespace Data.level
{
    public class LevelInitData
    {
        public List<CharacterData> Characters { get; private set; }
        public List<MonsterSpawnData> MonsterSpawnList { get; private set; }
        public int LevelId { get; private set; }

        public LevelInitData(List<CharacterData> characters, List<MonsterSpawnData> monsterSpawnData, int levelId)
        {
            Characters = characters ?? new List<CharacterData>();
            MonsterSpawnList = monsterSpawnData ?? new List<MonsterSpawnData>();
            LevelId = levelId;
        }
    }
}