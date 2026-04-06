/*
* ┌──────────────────────────────────┐
* │  描    述: 加载界面提示Text                      
* │  类    名: LoadingTextListSO.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace Data
{
    [CreateAssetMenu(menuName = "数据配置/DataBase/LoadingDataList", fileName = "LoadingTextData")]
    public class LoadingTextListSO  : ScriptableObject
    {
        public List<LoadingText> dataList;

        public LoadingText GetRandomTip()
        {
            if (dataList == null || dataList.Count == 0)
                return null;

            int randomIndex = UnityEngine.Random.Range(0, dataList.Count);
            return dataList[randomIndex];
        }
    }
    
    [Serializable]
    public class LoadingText
    {
        public Sprite bg;
        public string title;
        [TextArea]public string detail;
    }
}