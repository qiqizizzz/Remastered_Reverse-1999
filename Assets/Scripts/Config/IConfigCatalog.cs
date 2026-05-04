/*
* ┌──────────────────────────────────┐
* │  描    述: 配置目录接口                      
* │  类    名: IConfigCatalog.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;

namespace Config
{
    public interface IConfigCatalog<T> where T : class
    {
        void Init(IEnumerable<T> items);
        T Get(int id);
        IReadOnlyList<T> GetAll();
    }
}