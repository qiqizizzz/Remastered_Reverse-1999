/*
* ┌────────────────────────────────────────┐
* │  描    述: 战斗场景管理器（生成玩家与敌人等）                      
* │  类    名: EntityManager.cs       
* │  创    建: By qiqizizzz
* └────────────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Common;
using Common.Defines;
using Data.card;
using Data.level;
using Module.level;
using UnityEngine;

namespace Module.Character
{
    public class EntityManager
    {
        private readonly List<HeroEntity> AliveHeroes;
        private readonly List<EnemyEntity> AliveEnemies;
        
        private Dictionary<int, Vector3> heroSpawnPositions = new Dictionary<int, Vector3>()
        {
            {1, new Vector3(8f, -1.5f, 0)},
            {2, new Vector3(6.5f, -1.25f, 0)},
            {3, new Vector3(5f, -1.5f, 0)},
            {4, new Vector3(3.5f, -1.25f, 0)}
        };
        private Dictionary<int, Vector3> enemySpawnPositions = new Dictionary<int, Vector3>()
        {
            {1, new Vector3(-8f, -1.25f, 0)},
            {2, new Vector3(-6.5f, -1.5f, 0)},
            {3, new Vector3(-5f, -1.25f, 0)}
        };
        
        private int hasSpawnedHeroCount = 0;//已生成的英雄数量
        private int hasSpawnedEnemyCount = 0;//已生成的敌人数量
        private int totalRound;//总轮数（根据关卡数据计算得出）

        public EntityManager()
        {
            AliveHeroes = new List<HeroEntity>();
            AliveEnemies = new List<EnemyEntity>();
        }

        public int GetTotalRound() => totalRound;
        public List<HeroEntity> GetAliveHeroes() => AliveHeroes;
        public List<EnemyEntity> GetAliveEnemies() => AliveEnemies;
        
        #region 查找实体
        public BaseCharacter GetCharacterById(int id)
        {
            foreach (var hero in AliveHeroes)
            {
                if (hero.CharacterData.Id == id) return hero;
            }

            foreach (var enemy in AliveEnemies)
            {
                if (enemy.CharacterData.Id == id) return enemy;
            }

            return null;
        }
        
        public BaseCharacter GetCharacterByInstanceId(string instanceId)
        {
            foreach (var hero in AliveHeroes)
            {
                if (string.Equals(instanceId, hero.InstanceID)) 
                    return hero;
            }

            foreach (var enemy in AliveEnemies)
            {
                if (string.Equals(instanceId, enemy.InstanceID))
                    return enemy;
            }

            return null;
        }
        #endregion
        
        #region 生成实体与配置数据
        //生成玩家与敌人等
        public void SpawnBattleEntities(LevelInitData levelInitData, Action onComplete)
        {
            ClearBattleEntities();//清理上一轮数据
            
            List<CharacterData> heroDataList = levelInitData.Characters;
            List<MonsterSpawnData> monsterSpawnList = levelInitData.MonsterSpawnList;

            #region 获取生成数目
            int heroSpawnCount = 0;
            for (int i = 0; i < heroDataList.Count; i++)
            {
                if (heroDataList[i] != null && !string.IsNullOrEmpty(heroDataList[i].En_Name))
                {
                    heroSpawnCount++;
                }
            }
            
            int enemySpawnCount = 0;
            List<CharacterData> enemyDataList = new List<CharacterData>();
            for (int i = 0; i < monsterSpawnList.Count; i++)
            {
                if (monsterSpawnList[i] != null)
                {
                    enemySpawnCount += monsterSpawnList[i].count;
                    for (int j = 0; j < monsterSpawnList[i].count; j++)
                    {
                        enemyDataList.Add(GameApp.ConfigManager.GetEnemyData(monsterSpawnList[i].monsterId));
                    }
                }
            }
            #endregion
            
            #region 生成英雄
            for (int h = 0; h < heroDataList.Count; h++)
            { 
                int hIndex = h;//避免闭包问题
                if (heroDataList[hIndex] == null || string.IsNullOrEmpty(heroDataList[hIndex].En_Name)) continue;
                
                string heroRes = AddressDefines.Character_Hero + heroDataList[hIndex].En_Name;
                
                ResManager.InstantiateAsync(heroRes, (hGo) =>
                {
                    setHeroEntityData(hGo, heroDataList[hIndex]);
                    
                    if (AliveHeroes.Count == heroSpawnCount && AliveEnemies.Count == enemySpawnCount)
                    {
                        onComplete?.Invoke();
                    }
                });
            }
            #endregion

            #region 生成敌人(不同轮次)
            for (int e = 0; e < enemyDataList.Count; e++)
            { 
                int eIndex = e;//避免闭包问题
                if (enemyDataList[eIndex] == null || string.IsNullOrEmpty(enemyDataList[eIndex].En_Name)) continue;
                
                string enemyRes = AddressDefines.Character_Enemy + enemyDataList[eIndex].En_Name;
                
                ResManager.InstantiateAsync(enemyRes, (eGo) =>
                {
                    setEnemyEntityData(eGo, enemyDataList[eIndex]);
                    
                    if (AliveHeroes.Count == heroSpawnCount && AliveEnemies.Count == enemySpawnCount)
                    {
                        onComplete?.Invoke();
                    }
                });
            }
            #endregion
            
            if (enemySpawnCount == 0 && heroSpawnCount == 0)
            {
                onComplete?.Invoke();
                return;
            }
            
            totalRound = enemySpawnCount / 3;
            if (enemySpawnCount % 3 != 0)
            {
                totalRound += 1;
            }
            
            Debug.Log("生成玩家与敌人等，关卡id：" + levelInitData.LevelId);
        }

        private void ClearBattleEntities()
        {
            // 回收敌人对象
            foreach (var enemy in AliveEnemies)
            {
                string res = AddressDefines.Character_Enemy + enemy.CharacterData.En_Name;
                ResManager.ReleaseToPool(res, enemy.gameObject);
            }
            
            hasSpawnedHeroCount = 0;
            hasSpawnedEnemyCount = 0;
            
            AliveHeroes.Clear();
            AliveEnemies.Clear();
        }

        private void setHeroEntityData(GameObject go, CharacterData heroData)
        {
            HeroEntity heroEntity = go.GetComponent<HeroEntity>();
            heroEntity.Init(heroData);
            go.transform.position = heroSpawnPositions[++hasSpawnedHeroCount];//设置生成位置
            
            AliveHeroes.Add(heroEntity);
        }

        private void setEnemyEntityData(GameObject go, CharacterData enemyData)
        {
            EnemyEntity enemyEntity = go.GetComponent<EnemyEntity>();
            enemyEntity.Init(enemyData);
            
            int temp = hasSpawnedEnemyCount;
            go.transform.position = enemySpawnPositions[(temp % 3) + 1];
            hasSpawnedEnemyCount++;

            AliveEnemies.Add(enemyEntity);
            
            //隐藏超过3只的敌人
            if(hasSpawnedEnemyCount > 3)
                go.SetActive(false);
        }
        #endregion
    }
}