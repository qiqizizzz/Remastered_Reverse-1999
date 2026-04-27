/*
* ┌──────────────────────────────────┐
* │  描    述: 关卡初始化数据                      
* │  类    名: LevelInitData.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Data.card;
using MVC.Model;

namespace Data.level
{
    /// <summary>
    /// 怪物生成数据(种类与数量
    /// </summary>
    [Serializable]
    public class MonsterSpawnData
    {
        public int monsterId;
        public int count;
    }
    public class LevelModel : BaseModel
    {
        public List<CharacterDataSO> Characters { get; private set; }
        public List<MonsterSpawnData> MonsterSpawnList { get; private set; }
        public int LevelId { get; private set; }

        public LevelModel(List<CharacterDataSO> characters, List<MonsterSpawnData> monsterSpawnData, int levelId)
        {
            Characters = characters ?? new List<CharacterDataSO>();
            MonsterSpawnList = monsterSpawnData ?? new List<MonsterSpawnData>();
            LevelId = levelId;
        }
    }
}