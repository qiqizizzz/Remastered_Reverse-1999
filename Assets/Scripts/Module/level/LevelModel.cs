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
        public CharacterData Monster;
        public int Count;
    }
    
    [Serializable]
    public class LevelData
    {
        public int Id;
        public string Name;
        public string Description;
        public List<MonsterSpawnData> MonsterSpawns;//关卡中会生成的怪物
    }
    
    public class LevelModel : BaseModel
    {
        private Dictionary<int, LevelData> levels;

        public LevelModel()
        {
            levels = new Dictionary<int, LevelData>();
        }

        public override void Init()
        {
            Debug.Log("初始化数据中...");
            //TODO: 从配置文件或服务器加载关卡数据
        }
    }
}