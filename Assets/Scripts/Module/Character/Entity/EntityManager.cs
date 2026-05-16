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
        private readonly List<BaseCharacter> AliveEnemies;
        
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
            {3, new Vector3(-5f, -1.25f, 0)},
            {4, new Vector3(-3.5f, -1.5f, 0)}
        };
        
        private int hasSpawnedHeroCount = 0;//已生成的英雄数量
        private int hasSpawnedEnemyCount = 0;//已生成的敌人数量
        private int totalRound;//总轮数（根据关卡数据计算得出）

        public EntityManager()
        {
            AliveHeroes = new List<HeroEntity>();
            AliveEnemies = new List<BaseCharacter>();
        }

        public int GetTotalRound() => totalRound;
        public List<HeroEntity> GetAliveHeroes() => AliveHeroes;
        public List<BaseCharacter> GetAliveEnemies() => AliveEnemies;

        // 根据服务端快照生成PvP实体
        public void SpawnPvpBattleEntities(GameProtocol.BattleStateSnapshot snapshot, Action onComplete)
        {
            ClearBattleEntities();

            int heroSpawnCount = 0;
            int enemySpawnCount = 0;
            for (int i = 0; i < snapshot.Entities.Count; i++)
            {
                if (snapshot.Entities[i].IsPlayerSide)
                    heroSpawnCount++;
                else
                    enemySpawnCount++;
            }

            for (int i = 0; i < snapshot.Entities.Count; i++)
            {
                GameProtocol.CombatEntityInfo entityInfo = snapshot.Entities[i];
                CharacterDataSO characterData = GameApp.ConfigManager.Character.Get(entityInfo.ConfigId);
                if (characterData == null || string.IsNullOrEmpty(characterData.En_Name)) continue;

                if (entityInfo.IsPlayerSide)
                    spawnPvpHero(characterData, entityInfo, heroSpawnCount, enemySpawnCount, onComplete);
                else
                    spawnPvpEnemy(characterData, entityInfo, heroSpawnCount, enemySpawnCount, onComplete);
            }

            if (heroSpawnCount == 0 && enemySpawnCount == 0)
                onComplete?.Invoke();
        }
        
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
        
        public BaseCharacter GetCharacterByCombatInstanceId(int combatInstanceId)
        {
            foreach (var hero in AliveHeroes)
            {
                if (hero.CombatInstanceId == combatInstanceId)
                    return hero;
            }

            foreach (var enemy in AliveEnemies)
            {
                if (enemy.CombatInstanceId == combatInstanceId)
                    return enemy;
            }

            return null;
        }
        #endregion
        
        #region 生成实体与配置数据
        //生成玩家与敌人等
        public void SpawnBattleEntities(LevelModel levelModel, Action onComplete)
        {
            ClearBattleEntities();//清理上一轮数据
            
            List<CharacterDataSO> heroDataList = levelModel.Characters;
            List<MonsterSpawnData> monsterSpawnList = levelModel.MonsterSpawnList;

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
            List<CharacterDataSO> enemyDataList = new List<CharacterDataSO>();
            for (int i = 0; i < monsterSpawnList.Count; i++)
            {
                if (monsterSpawnList[i] != null)
                {
                    enemySpawnCount += monsterSpawnList[i].count;
                    for (int j = 0; j < monsterSpawnList[i].count; j++)
                    {
                        enemyDataList.Add(GameApp.ConfigManager.Character.Get(monsterSpawnList[i].monsterId));
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
            
            QLog.Info("生成玩家与敌人等，关卡id：" + levelModel.LevelId);
        }

        private void ClearBattleEntities()
        {
            BaseCharacter.ResetCombatInstanceIdCounter();

            // 回收敌人对象
            foreach (var enemy in AliveEnemies)
            {
                if (enemy == null || enemy.gameObject == null) continue;
                
                string res = AddressDefines.Character_Enemy + enemy.CharacterData.En_Name;
                ResManager.ReleaseToPool(res, enemy.gameObject);
            }
            
            hasSpawnedHeroCount = 0;
            hasSpawnedEnemyCount = 0;
            
            AliveHeroes.Clear();
            AliveEnemies.Clear();
        }

        private void setHeroEntityData(GameObject go, CharacterDataSO heroData)
        {
            HeroEntity heroEntity = go.GetComponent<HeroEntity>();
            heroEntity.SetEnemySide(false);
            heroEntity.Init(heroData);
            go.transform.position = heroSpawnPositions[++hasSpawnedHeroCount];//设置生成位置

            AliveHeroes.Add(heroEntity);
        }

        private void setEnemyEntityData(GameObject go, CharacterDataSO enemyData)
        {
            BaseCharacter enemyEntity = go.GetComponent<BaseCharacter>();
            enemyEntity.SetEnemySide(true);
            enemyEntity.Init(enemyData);

            int temp = hasSpawnedEnemyCount;
            go.transform.position = enemySpawnPositions[(temp % 3) + 1];
            hasSpawnedEnemyCount++;

            AliveEnemies.Add(enemyEntity);

            //隐藏超过3只的敌人
            if(hasSpawnedEnemyCount > 3)
                go.SetActive(false);
        }

        // 生成PvP己方英雄
        private void spawnPvpHero(CharacterDataSO characterData, GameProtocol.CombatEntityInfo entityInfo, int heroSpawnCount, int enemySpawnCount, Action onComplete)
        {
            string heroRes = AddressDefines.Character_Hero + characterData.En_Name;
            ResManager.InstantiateAsync(heroRes, (go) =>
            {
                HeroEntity heroEntity = go.GetComponent<HeroEntity>();
                heroEntity.SetEnemySide(false);
                heroEntity.Init(characterData);
                heroEntity.SetCombatInstanceId(entityInfo.InstanceId);
                heroEntity.SetHpFromSnapshot(entityInfo.CurrentHp, entityInfo.MaxHp);
                heroEntity.SetActionPoint(entityInfo.ActionPoint);
                go.transform.position = heroSpawnPositions[++hasSpawnedHeroCount];
                AliveHeroes.Add(heroEntity);

                if (AliveHeroes.Count == heroSpawnCount && AliveEnemies.Count == enemySpawnCount)
                    onComplete?.Invoke();
            });
        }

        // 生成PvP敌方单位
        private void spawnPvpEnemy(CharacterDataSO characterData, GameProtocol.CombatEntityInfo entityInfo, int heroSpawnCount, int enemySpawnCount, Action onComplete)
        {
            string enemyRes = AddressDefines.Character_Hero + characterData.En_Name;
            ResManager.InstantiateAsync(enemyRes, (go) =>
            {
                BaseCharacter enemyEntity = go.GetComponent<BaseCharacter>();
                enemyEntity.SetEnemySide(true);
                enemyEntity.Init(characterData);
                enemyEntity.SetCombatInstanceId(entityInfo.InstanceId);
                enemyEntity.SetHpFromSnapshot(entityInfo.CurrentHp, entityInfo.MaxHp);
                go.transform.position = enemySpawnPositions[++hasSpawnedEnemyCount];
                AliveEnemies.Add(enemyEntity);

                if (AliveHeroes.Count == heroSpawnCount && AliveEnemies.Count == enemySpawnCount)
                    onComplete?.Invoke();
            });
        }
        #endregion
    }
}
