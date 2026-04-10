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
            
            List<CharacterData> heroDataList = levelInitData.Characters;

            #region 生成角色
            int targetSpawnCount = 0;
            for (int i = 0; i < heroDataList.Count; i++)
            {
                if (heroDataList[i] != null && !string.IsNullOrEmpty(heroDataList[i].Name))
                {
                    targetSpawnCount++;
                }
            }

            if (targetSpawnCount == 0)
            {
                onComplete?.Invoke();
                return;
            }
            
            for (int i = 0; i < heroDataList.Count; i++)
            { 
                int index = i;//避免闭包问题
                if (heroDataList[index] == null || string.IsNullOrEmpty(heroDataList[index].Name)) continue;
                
                string res = AddressDefines.Character_Hero + heroDataList[index].Name;
                
                ResManager.InstantiateAsync(res, (go) =>
                {
                    setHeroEntityData(go, heroDataList[index]);
                    
                    //&& AliveEnemies.Count == levelInitData.MonsterSpawnList.Count
                    if (AliveHeroes.Count == targetSpawnCount)
                    {
                        onComplete?.Invoke();
                    }
                });
            }
            #endregion
            
            //TODO：生成Enemy
            
            
            Debug.Log("生成玩家与敌人等，关卡id：" + levelInitData.LevelId);
        }

        public void ClearBattleEntities()
        {
            //TODO: 这里应该销毁之前生成的实体对象，目前先清空列表
            
            AliveHeroes.Clear();
            AliveEnemies.Clear();
        }

        private void setHeroEntityData(GameObject go, CharacterData characterData)
        {
            HeroEntity heroEntity = go.GetComponent<HeroEntity>();
            heroEntity.Init(characterData);
            
            AliveHeroes.Add(heroEntity);
        }

        private void setEnemyEntityData(GameObject go, CharacterData characterData)
        {
            EnemyEntity enemyEntity = go.GetComponent<EnemyEntity>();
            enemyEntity.Init(characterData);

            AliveEnemies.Add(enemyEntity);
        }
    }
}