/*
* ┌──────────────────────────────────┐
* │  描    述: 关卡目录                      
* │  类    名: LevelCatalog.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using System.Linq;
using Data.card;
using Data.level;

namespace Config.Catalogs
{
    public interface ILevelCatalog : IConfigCatalog<LevelDataSO>
    {
        List<MonsterSpawnData> GetLevelMonsterSpawnData(int levelId);
    }
    
    public class LevelCatalog : ILevelCatalog
    {
        private readonly Dictionary<int, LevelDataSO> levelConfig = new Dictionary<int, LevelDataSO>();
        
        public void Init(IEnumerable<LevelDataSO> items)
        {
            levelConfig.Clear();
            foreach (var item in items)
            {
                if(item != null) levelConfig[item.Id] = item;
            }
        }

        public LevelDataSO Get(int id) => levelConfig.TryGetValue(id, out var data) ? data : null;

        public IReadOnlyList<LevelDataSO> GetAll() => levelConfig.Values.ToList();

        public List<MonsterSpawnData> GetLevelMonsterSpawnData(int levelId)
        {
            if (levelConfig.TryGetValue(levelId, out LevelDataSO levelData))
            {
                return levelData.MonsterSpawns;
            }
            return new List<MonsterSpawnData>();
        }
    }
}