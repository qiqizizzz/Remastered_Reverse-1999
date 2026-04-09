/*
* ┌────────────────────────────────────────┐
* │  描    述: 战斗场景管理器（生成玩家与敌人等）                      
* │  类    名: EntityManager.cs       
* │  创    建: By qiqizizzz
* └────────────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Data.level;
using UnityEngine;

namespace Module.Character
{
    public class EntityManager
    {
        public List<HeroEntity> AliveHeroes { get; private set; }
        public List<EnemyEntity> AliveEnemies { get; private set; }

        public EntityManager()
        {
            AliveHeroes = new List<HeroEntity>();
            AliveEnemies = new List<EnemyEntity>();
        }

        //生成玩家与敌人等
        public void SpawnBattleEntities(LevelInitData levelInitData, Action onComplete)
        {
            ClearBattleEntities();//清理上一轮数据
            
            Debug.Log("生成玩家与敌人等，关卡id：" + levelInitData.LevelId);
        }

        public void ClearBattleEntities()
        {
            //TODO: 这里应该销毁之前生成的实体对象，目前先清空列表
            
            AliveHeroes.Clear();
            AliveEnemies.Clear();
        }
    }
}