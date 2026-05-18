/*
* ┌──────────────────────────────────┐
* │  描    述: 红点预处理烘焙数据(序列化保存用)                      
* │  类    名: RedDotBakeData.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using UnityEngine;

namespace Module.RedDot.Component
{
    [Serializable]
    public class RedDotBakeData
    {
        [Tooltip("红点的树形路径, 用'/'分隔")]
        public string NodePath;
        [Tooltip("红点UI对象")]
        public GameObject DotObj;
    }
}