/*
* ┌──────────────────────────────────┐
* │  描    述: 配置管理器                      
* │  类    名: ConfigManager.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Defines;
using Config.Catalogs;
using Data;
using Data.card;
using Data.level;
using Module.level;
using UnityEngine;

namespace Config
{
    public class ConfigManager
    {
        public ICardCatalog Card { get; }
        public ICharacterCatalog Character { get; }
        public ILevelCatalog Level { get; }

        public ConfigManager()
        {
            Card = new CardCatalog();
            Character = new CharacterCatalog();
            Level = new LevelCatalog();
        }

        public async Task LoadAllConfigsAsync()
        {
            var tcs = new TaskCompletionSource<GameConfigDatabase>();
            
            ResManager.LoadAssetAsync<GameConfigDatabase>(AddressDefines.Data_GameConfigDatabase, database =>
            {
                tcs.SetResult(database);
            });
            
            GameConfigDatabase db = await tcs.Task;

            if (db == null)
            {
                Debug.LogError("加载配置数据库失败!");
                return;
            }
            
            Card.Init(db.allHeroCards.Concat(db.allEnemyCards));
            Character.Init(db.allHeroes.Concat(db.allEnemies));
            Level.Init(db.allLevels);
            
            Debug.Log($"加载完成: {Card.GetAll().Count}张卡牌, {Character.GetAll().Count}个角色, {Level.GetAll().Count}个关卡");
        }
    }
}