/*
 * ┌─────────────────────────────────────────────────────────┐
 * │  描    述: 游戏数据编辑器窗口，支持编辑卡牌、角色、敌人和关卡配置
 * │  类    名: GameDataEditorWindow.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────────────────┘
 */

using UnityEditor;
using UnityEngine;
using System.IO;
using Data;
using Data.card;

public class GameDataEditorWindow : EditorWindow
{
    private GameConfigDatabase database;
    private SerializedObject serializedDatabase;

    private int currentTab = 0; 
    private readonly string[] tabNames = { "💳 卡牌 (Cards)", "🦸 角色 (Characters)", "👿 敌人 (Enemies)", "🗺️ 关卡 (Levels)" };
    
    private int selectedIndex = -1;
    private Vector2 leftScrollPos;
    private Vector2 rightScrollPos;

    // 自定义 GUI 样式
    private GUIStyle sidebarStyle;
    private GUIStyle selectedBtnStyle;
    private GUIStyle normalBtnStyle;
    private GUIStyle headerStyle;
    private GUIStyle panelStyle;

    // 新建数据存放的默认根目录，确保这个目录在你的工程里存在！
    private readonly string DATA_ROOT_PATH = "Assets/RemoteAssets/Data";

    [MenuItem("Tools/游戏数据编辑器", priority = 0)]
    public static void ShowWindow()
    {
        var window = GetWindow<GameDataEditorWindow>("数据编辑器");
        window.minSize = new Vector2(900, 600);
        window.Show();
    }

    private void OnEnable()
    {
        LoadDatabase();
    }

    private void LoadDatabase()
    {
        string[] guids = AssetDatabase.FindAssets("t:GameConfigDatabase");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            database = AssetDatabase.LoadAssetAtPath<GameConfigDatabase>(path);
            if (database != null)
            {
                serializedDatabase = new SerializedObject(database);
            }
        }
    }

    private void OnGUI()
    {
        if (database == null || serializedDatabase == null)
        {
            EditorGUILayout.HelpBox("未能找到 GameConfigDatabase! 请确保它在工程中存在。", MessageType.Error);
            if (GUILayout.Button("重试加载", GUILayout.Height(30))) LoadDatabase();
            return;
        }

        InitStyles();
        serializedDatabase.Update();

        DrawTopTabs();

        EditorGUILayout.BeginHorizontal();
        {
            DrawLeftSidebar();
            DrawVerticalLine(); 
            DrawRightPanel();
        }
        EditorGUILayout.EndHorizontal();

        serializedDatabase.ApplyModifiedProperties();
    }

    #region UI 绘制

    private void DrawTopTabs()
    {
        EditorGUILayout.BeginVertical(panelStyle);
        GUILayout.Space(5);
        EditorGUI.BeginChangeCheck();
        
        // 美化 Toolbar
        GUI.backgroundColor = new Color(0.9f, 0.9f, 0.9f);
        currentTab = GUILayout.Toolbar(currentTab, tabNames, GUILayout.Height(35));
        GUI.backgroundColor = Color.white;

        if (EditorGUI.EndChangeCheck())
        {
            selectedIndex = -1;
            GUI.FocusControl(null); 
        }
        GUILayout.Space(5);
        EditorGUILayout.EndVertical();
    }

    private void DrawLeftSidebar()
    {
        EditorGUILayout.BeginVertical(sidebarStyle, GUILayout.Width(260), GUILayout.ExpandHeight(true));
        
        SerializedProperty listProperty = GetCurrentListProperty();

        // 左侧工具栏
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label($" 列表总数: {listProperty?.arraySize ?? 0}", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        
        if (GUILayout.Button("清理空值", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            CleanNullEntries(listProperty);
            GUIUtility.ExitGUI(); 
        }
        
        GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f);
        if (GUILayout.Button("+ 新建", EditorStyles.toolbarButton, GUILayout.Width(50)))
        {
            CreateNewData();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        if (listProperty == null)
        {
            EditorGUILayout.HelpBox("未找到对应的数据列表，请检查 GameConfigDatabase 中的变量命名是否正确。", MessageType.Warning);
            EditorGUILayout.EndVertical();
            return;
        }

        // 列表主体
        leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos);
        for (int i = 0; i < listProperty.arraySize; i++)
        {
            SerializedProperty elementProp = listProperty.GetArrayElementAtIndex(i);
            bool isNull = elementProp.objectReferenceValue == null;
            
            // 隔行变色美化
            Rect rowRect = EditorGUILayout.BeginHorizontal();
            if (i % 2 == 0)
            {
                EditorGUI.DrawRect(rowRect, new Color(0, 0, 0, 0.05f)); // 奇偶行底色
            }
            if (selectedIndex == i)
            {
                EditorGUI.DrawRect(rowRect, new Color(0.15f, 0.45f, 0.8f, 0.2f)); // 选中行底色
            }

            string displayName = isNull ? "⚠️ 空 (Null)" : $"[{i}]  {elementProp.objectReferenceValue.name}";
            GUIStyle btnStyle = (selectedIndex == i) ? selectedBtnStyle : normalBtnStyle;

            if (GUILayout.Button(displayName, btnStyle))
            {
                selectedIndex = i;
                GUI.FocusControl(null); 
            }

            if (isNull)
            {
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(30)))
                {
                    listProperty.DeleteArrayElementAtIndex(i);
                    serializedDatabase.ApplyModifiedProperties();
                    if (selectedIndex == i) selectedIndex = -1;
                    GUIUtility.ExitGUI();
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawVerticalLine()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(1));
        rect.height = position.height;
        EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f, 0.5f)); // 更柔和的分割线
    }

    private void DrawRightPanel()
    {
        EditorGUILayout.BeginVertical(panelStyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        
        SerializedProperty listProperty = GetCurrentListProperty();

        if (listProperty != null && selectedIndex >= 0 && selectedIndex < listProperty.arraySize)
        {
            SerializedProperty elementProp = listProperty.GetArrayElementAtIndex(selectedIndex);
            
            if (elementProp.objectReferenceValue != null)
            {
                // 右侧标题栏
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                GUILayout.Label($" 📝 当前编辑:  {elementProp.objectReferenceValue.name}", headerStyle);
                GUILayout.FlexibleSpace(); 
                
                if (GUILayout.Button("🔍 定位", EditorStyles.toolbarButton, GUILayout.Width(50)))
                {
                    EditorGUIUtility.PingObject(elementProp.objectReferenceValue); 
                }
                
                GUI.backgroundColor = new Color(0.6f, 0.8f, 1f);
                if (GUILayout.Button("💾 保存", EditorStyles.toolbarButton, GUILayout.Width(50)))
                {
                    SaveData();
                }
                
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f); 
                if (GUILayout.Button("🗑️ 删除", EditorStyles.toolbarButton, GUILayout.Width(50)))
                {
                    DeleteCurrentData();
                }
                GUI.backgroundColor = Color.white; 
                EditorGUILayout.EndHorizontal();

                // 绘制属性详情
                rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos);
                GUILayout.Space(10);
                
                SerializedObject itemObj = new SerializedObject(elementProp.objectReferenceValue);
                itemObj.Update();

                SerializedProperty iterator = itemObj.GetIterator();
                bool enterChildren = true;
                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (iterator.name == "m_Script") continue; 
                    
                    // 加点内边距让属性看着不那么拥挤
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(10);
                    EditorGUILayout.PropertyField(iterator, true);
                    GUILayout.Space(10);
                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(2);
                }
                itemObj.ApplyModifiedProperties();
                
                GUILayout.Space(20);
                EditorGUILayout.EndScrollView();
            }
            else
            {
                DrawMissingDataWarning(listProperty);
            }
        }
        else
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("👈 请在左侧选择或新建一个配置项", EditorStyles.largeLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawMissingDataWarning(SerializedProperty listProperty)
    {
        GUILayout.Space(20);
        EditorGUILayout.HelpBox("该数据已丢失或为空引用 (Null)！\n请移除此无效项以保持数据整洁。", MessageType.Warning);
        GUILayout.Space(10);
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("移除该空数据", GUILayout.Width(150), GUILayout.Height(35)))
        {
            listProperty.DeleteArrayElementAtIndex(selectedIndex);
            serializedDatabase.ApplyModifiedProperties();
            selectedIndex = -1;
            GUIUtility.ExitGUI();
        }
        GUI.backgroundColor = Color.white;
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region 功能逻辑

    private SerializedProperty GetCurrentListProperty()
    {
        // 增加了一个 allEnemies，对应你的 GameConfigDatabase 字段
        string propertyName = currentTab switch
        {
            0 => "allCards",
            1 => "allCharacters",
            2 => "allEnemies", // 敌人独立列表
            3 => "allLevels",
            _ => "allCards"
        };
        return serializedDatabase.FindProperty(propertyName);
    }

    private void CleanNullEntries(SerializedProperty listProp)
    {
        if (listProp == null) return;
        for (int i = listProp.arraySize - 1; i >= 0; i--)
        {
            if (listProp.GetArrayElementAtIndex(i).objectReferenceValue == null)
            {
                listProp.DeleteArrayElementAtIndex(i);
            }
        }
        serializedDatabase.ApplyModifiedProperties();
        selectedIndex = -1;
    }

    private void CreateNewData()
    {
        SerializedProperty listProp = GetCurrentListProperty();
        if (listProp == null) return;
        
        int newId = 0;
        
        // 倒序查找最后一个有效的 SO，获取它的 ID，递增生成新 ID
        for (int i = listProp.arraySize - 1; i >= 0; i--)
        {
            SerializedProperty elementProp = listProp.GetArrayElementAtIndex(i);
            if (elementProp.objectReferenceValue != null)
            {
                SerializedObject lastObj = new SerializedObject(elementProp.objectReferenceValue);
                SerializedProperty idProp = lastObj.FindProperty("Id"); 
                if (idProp != null)
                {
                    newId = idProp.intValue + 1;
                    break;
                }
            }
        }

        // 默认起始 ID
        if (newId <= 0)
        {
            newId = currentTab switch
            {
                0 => 2001,  // 卡牌
                1 => 1001,  // 角色
                2 => 5001,  // 敌人 (独立起始段，防止与角色混淆)
                3 => 1,     // 关卡
                _ => 1
            };
        }

        // 确定文件夹名
        string subFolder = currentTab switch
        {
            0 => "Card",
            1 => "Character",
            2 => "Enemy", // 新增敌人存放文件夹
            3 => "Level",
            _ => "Card"
        };
        
        string folderPath = $"{DATA_ROOT_PATH}/{subFolder}";

        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        string fileName = $"{subFolder}_{newId}.asset";
        string fullPath = $"{folderPath}/{fileName}";

        // 实例化数据对象（敌人和角色一样使用 CharacterData）
        ScriptableObject newAsset = null;
        if (currentTab == 0) newAsset = CreateInstance<CardData>();
        else if (currentTab == 1 || currentTab == 2) newAsset = CreateInstance<CharacterData>();
        else newAsset = CreateInstance<LevelData>();

        AssetDatabase.CreateAsset(newAsset, fullPath);

        // 设置新生成的 ID
        SerializedObject newAssetObj = new SerializedObject(newAsset);
        SerializedProperty newIdProp = newAssetObj.FindProperty("Id"); 
        if (newIdProp != null)
        {
            newIdProp.intValue = newId;
            newAssetObj.ApplyModifiedPropertiesWithoutUndo();
        }

        // 添加到列表
        listProp.arraySize++;
        SerializedProperty newElement = listProp.GetArrayElementAtIndex(listProp.arraySize - 1);
        newElement.objectReferenceValue = newAsset;

        serializedDatabase.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        selectedIndex = listProp.arraySize - 1;
        GUI.FocusControl(null);
    }

    private void DeleteCurrentData()
    {
        if (EditorUtility.DisplayDialog("⚠️ 危险操作", "确定要删除这条数据吗？对应的本地 .asset 文件也将被永久删除，且无法撤销！", "确认删除", "取消"))
        {
            SerializedProperty listProp = GetCurrentListProperty();
            SerializedProperty elementProp = listProp.GetArrayElementAtIndex(selectedIndex);
            
            Object assetToDelete = elementProp.objectReferenceValue;

            elementProp.objectReferenceValue = null;
            listProp.DeleteArrayElementAtIndex(selectedIndex);
            serializedDatabase.ApplyModifiedProperties();

            if (assetToDelete != null)
            {
                string path = AssetDatabase.GetAssetPath(assetToDelete);
                AssetDatabase.DeleteAsset(path);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            selectedIndex = -1;
        }
    }

    private void SaveData()
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=green>数据保存成功！</color>");
    }

    #endregion

    #region 美化样式初始化

    private void InitStyles()
    {
        if (sidebarStyle == null)
        {
            sidebarStyle = new GUIStyle("box");
            sidebarStyle.padding = new RectOffset(5, 5, 5, 5); 
            sidebarStyle.margin = new RectOffset(0, 0, 0, 0);
        }
        if (panelStyle == null)
        {
            panelStyle = new GUIStyle();
            panelStyle.padding = new RectOffset(5, 5, 5, 5);
        }
        if (normalBtnStyle == null)
        {
            normalBtnStyle = new GUIStyle(GUI.skin.button);
            normalBtnStyle.alignment = TextAnchor.MiddleLeft;
            normalBtnStyle.fixedHeight = 32;
            normalBtnStyle.margin = new RectOffset(2, 2, 2, 2);
            normalBtnStyle.richText = true;
        }
        if (selectedBtnStyle == null)
        {
            selectedBtnStyle = new GUIStyle(GUI.skin.button);
            selectedBtnStyle.alignment = TextAnchor.MiddleLeft;
            selectedBtnStyle.fixedHeight = 32;
            // 选中状态字体变亮加粗
            selectedBtnStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.4f, 0.8f, 1f) : new Color(0.1f, 0.4f, 0.8f);
            selectedBtnStyle.fontStyle = FontStyle.Bold;
            selectedBtnStyle.margin = new RectOffset(2, 2, 2, 2);
            selectedBtnStyle.richText = true;
        }
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 13;
            headerStyle.alignment = TextAnchor.MiddleLeft;
        }
    }

    #endregion
}