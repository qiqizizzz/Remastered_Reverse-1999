/*
 * ┌──────────────────────────────────┐
 * │  描    述: 红点自动化烘培工具
 * │  类    名: RedDotBaker.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;
using Common;
using Module.RedDot.Component;
using UnityEditor;
using UnityEngine;

namespace Config.Editor
{
    public class RedDotBaker : UnityEditor.Editor
    {
        private const string RED_DOT_PREFIX = "RedDot@";

        [MenuItem("一键烘培红点系统")]
        public static void BakeRedDots()
        {
            GameObject selectedObj = Selection.activeGameObject;

            if (selectedObj == null)
            {
                QLog.Warning("请先选择一个游戏对象作为红点系统的根节点");
                return;
            }

            //查找是否挂载了红点管家
            RedDotMonoBinder binder = selectedObj.GetComponent<RedDotMonoBinder>();
            if (binder == null)
            {
                binder = selectedObj.AddComponent<RedDotMonoBinder>();
            }

            binder.BakedDataList.Clear();

            Transform[] allTransforms = selectedObj.GetComponentsInChildren<Transform>(true);

            int backCount = 0;
            foreach (var t in allTransforms)
            {
                if (t.name.StartsWith(RED_DOT_PREFIX))
                {
                    string path = t.name.Substring(RED_DOT_PREFIX.Length);//排除前缀提取路径
                    
                    binder.BakedDataList.Add(new RedDotBakeData()
                    {
                        NodePath = path,
                        DotObj = t.gameObject
                    });

                    backCount++;
                }
            }
            
            EditorUtility.SetDirty(binder);//标记该预制体已被修改
            PrefabUtility.RecordPrefabInstancePropertyModifications(binder);//应用在Prefab上
            
            QLog.Info($"<color=green>红点烘培完成！共找到 {backCount} 个红点对象并生成绑定数据。</color>");
        }
    }
}