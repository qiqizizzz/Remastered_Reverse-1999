/*
* ┌──────────────────────────────────────────┐
* │  描    述: 关卡数据(记录会出现的怪物种类和数量等)                      
* │  类    名: LevelModel.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Data.card;
using MVC.Model;
using UnityEngine;

namespace Module.level
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

    [Serializable]
    public class LevelDataArray
    {
        public List<LevelData> levels;
    }
    
    [Serializable]
    public class LevelData
    {
        public int id;
        public string name;
        public string description;//形式为xxx-xxx-xxx,每段描述一个阶段,用-分隔
        public List<MonsterSpawnData> monsterSpawns;//关卡中会生成的怪物
    }
    
    public class LevelModel : BaseModel
    {
        public LevelModel()
        {
            
        }
    }
}