/*
* ┌──────────────────────────────────┐
* │  描    述: 关卡数据                      
* │  类    名: LevelData.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Module.level;
using UnityEngine;

namespace Data.card
{
    [Serializable]
    [CreateAssetMenu(fileName = "NewLevelData", menuName = "数据配置/Data/Level")]
    public class LevelData : ScriptableObject
    {
        public int Id;
        public string Name;
        public string Description;//形式为xxx-xxx-xxx,每段描述一个阶段,用-分隔
        public List<MonsterSpawnData> MonsterSpawns;//关卡中会生成的怪物
    }
}