/*
* ┌──────────────────────────────────┐
* │  描    述: Json配置加载器
* │  类    名: JsonConfigLoader.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using UnityEngine;

namespace Config
{
    public class JsonConfigLoader
    {
        //加载json配置文件
        public static T Load<T>(string path)
        {
            TextAsset asset = Resources.Load<TextAsset>($"Configs/{path}");
            
            if (asset == null)
            {
                Debug.LogError($"Failed to load config at path: {path}");
                return default;
            }
            
            try
            {
                return JsonUtility.FromJson<T>(asset.text);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to parse config at path: {path}, error: {e.Message}");
                return default;
            }
        }
    }
}