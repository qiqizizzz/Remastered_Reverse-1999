/*
* ┌──────────────────────────────────┐
* │  描    述: 角色目录                      
* │  类    名: CharacterCatalog.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using System.Linq;
using Data.card;

namespace Config.Catalogs
{
    public interface ICharacterCatalog : IConfigCatalog<CharacterDataSO>
    {
        CharacterDataSO GetByName(string name);
    }
    
    public class CharacterCatalog : ICharacterCatalog
    {
        private readonly Dictionary<int, CharacterDataSO> characterConfig = new Dictionary<int, CharacterDataSO>();
        private readonly Dictionary<string, CharacterDataSO> characterNameConfig = new Dictionary<string, CharacterDataSO>();
        
        public void Init(IEnumerable<CharacterDataSO> items)
        {
            characterConfig.Clear();
            foreach (var item in items)
            {
                if (item != null)
                {
                    characterConfig[item.Id] = item;
                    characterNameConfig[item.Name] = item;
                }
            }
        }

        public CharacterDataSO GetByName(string name) => characterNameConfig.TryGetValue(name, out var data) ? data : null;
        
        public CharacterDataSO Get(int id)  => characterConfig.TryGetValue(id, out var data) ? data : null;

        public IReadOnlyList<CharacterDataSO> GetAll() => characterConfig.Values.ToList();
    }
}