/*
* ┌────────────────────────────────────────┐
* │  描    述: 战斗场景管理器（生成玩家与敌人等）                      
* │  类    名: EntityManager.cs       
* │  创    建: By qiqizizzz
* └────────────────────────────────────────┘
*/

using Data.level;
using UnityEngine;

namespace Module.fight
{
    public class EntityManager
    {

        //生成玩家与敌人等
        public void SpawnBattleEntities(LevelInitData levelInitData)
        {
            Debug.Log("生成玩家与敌人等，关卡id：" + levelInitData.LevelId);
        }
    }
}