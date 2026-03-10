/*
* ┌──────────────────────────────────┐
* │  描    述: 加载模块数据模型                      
* │  类    名: LoadingModel.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using MVC;
using MVC.Model;

namespace Module.Loading
{
    public class LoadingModel : BaseModel
    {
        public string SceneName;
        public System.Action callback;

        public LoadingModel()
        {
            
        }

        public void SetSceneName(string name) => SceneName = name;
    }
}