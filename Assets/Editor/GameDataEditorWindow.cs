/*
 * ┌─────────────────────────────────────────────────────────┐
 * │  描    述: 游戏数据编辑器窗口，支持编辑卡牌、角色和关卡配置
 * │  类    名: GameDataEditorWindow.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────────────────┘
 */

using Data;
using UnityEditor;
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
    private string[] tabNames = { "卡牌 (Cards)", "角色 (Characters)", "关卡 (Levels)" };
    
    private int selectedIndex = -1;
    private Vector2 leftScrollPos;
    private Vector2 rightScrollPos;

    private GUIStyle sidebarStyle;
    private GUIStyle selectedBtnStyle;
    private GUIStyle normalBtnStyle;

    // 新建数据存放的默认根目录，确保这个目录在你的工程里存在！
    private readonly string DATA_ROOT_PATH = "Assets/RemoteAssets/Data";

    [MenuItem("Tools/游戏数据编辑器", priority = 0)]
    public static void ShowWindow()
    {
        var window = GetWindow<GameDataEditorWindow>("数据编辑器");
        window.minSize = new Vector2(850, 550);
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
            if (GUILayout.Button("重试加载")) LoadDatabase();
            return;
        }

        InitStyles();
        serializedDatabase.Update();

        // 顶部大页签
        DrawTopTabs();

        // 主体左右分栏
        EditorGUILayout.BeginHorizontal();
        {
            DrawLeftSidebar();
            DrawVerticalLine(); // 画竖线
            DrawRightPanel();
        }
        EditorGUILayout.EndHorizontal();

        serializedDatabase.ApplyModifiedProperties();
    }

    #region UI 绘制

    private void DrawTopTabs()
    {
        GUILayout.Space(5);
        EditorGUI.BeginChangeCheck();
        currentTab = GUILayout.Toolbar(currentTab, tabNames, GUILayout.Height(30));
        if (EditorGUI.EndChangeCheck())
        {
            selectedIndex = -1;
            GUI.FocusControl(null); 
        }
        GUILayout.Space(5);
    }

    private void DrawLeftSidebar()
    {
        EditorGUILayout.BeginVertical(sidebarStyle, GUILayout.Width(250), GUILayout.ExpandHeight(true));
        
        SerializedProperty listProperty = GetCurrentListProperty();

        // 左侧工具栏：标题 + 新建按钮
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label($"列表 ({listProperty.arraySize})", EditorStyles.boldLabel);
        if (GUILayout.Button("+ 新建", EditorStyles.toolbarButton, GUILayout.Width(50)))
        {
            CreateNewData();
        }
        EditorGUILayout.EndHorizontal();

        leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos);

        for (int i = 0; i < listProperty.arraySize; i++)
        {
            SerializedProperty elementProp = listProperty.GetArrayElementAtIndex(i);
            
            string displayName = "空 (Null)";
            if (elementProp.objectReferenceValue != null)
            {
                displayName = $"[{i}] {elementProp.objectReferenceValue.name}";
            }

            GUIStyle btnStyle = (selectedIndex == i) ? selectedBtnStyle : normalBtnStyle;

            if (GUILayout.Button(displayName, btnStyle))
            {
                selectedIndex = i;
                GUI.FocusControl(null); 
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawVerticalLine()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(1));
        rect.height = position.height;
        EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f, 1f));
    }

    private void DrawRightPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        
        SerializedProperty listProperty = GetCurrentListProperty();

        if (selectedIndex >= 0 && selectedIndex < listProperty.arraySize)
        {
            SerializedProperty elementProp = listProperty.GetArrayElementAtIndex(selectedIndex);
            
            if (elementProp.objectReferenceValue != null)
            {
                // 右侧工具栏：定位、保存、删除
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                GUILayout.Label($"当前编辑: {elementProp.objectReferenceValue.name}", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace(); 
                
                if (GUILayout.Button("定位", EditorStyles.toolbarButton, GUILayout.Width(40)))
                {
                    EditorGUIUtility.PingObject(elementProp.objectReferenceValue); 
                }
                if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(40)))
                {
                    SaveData();
                }
                
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f); 
                if (GUILayout.Button("删除", EditorStyles.toolbarButton, GUILayout.Width(40)))
                {
                    DeleteCurrentData();
                }
                GUI.backgroundColor = Color.white; 
                EditorGUILayout.EndHorizontal();

                // 详细属性列表
                rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos);
                
                SerializedObject itemObj = new SerializedObject(elementProp.objectReferenceValue);
                itemObj.Update();

                EditorGUILayout.Space();
                SerializedProperty iterator = itemObj.GetIterator();
                bool enterChildren = true;
                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (iterator.name == "m_Script") continue; 
                    EditorGUILayout.PropertyField(iterator, true);
                }
                itemObj.ApplyModifiedProperties();
                
                EditorGUILayout.EndScrollView();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("请在左侧选择或新建一个配置项。", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }

    #endregion

    #region 功能逻辑 (新建、删除、保存)

    private SerializedProperty GetCurrentListProperty()
    {
        string propertyName = currentTab == 0 ? "allCards" : (currentTab == 1 ? "allCharacters" : "allLevels");
        return serializedDatabase.FindProperty(propertyName);
    }

    private void CreateNewData()
    {
        SerializedProperty listProp = GetCurrentListProperty();
        
        // 1. 确定前一个 ID 以实现自增
        int newId = 0;
        
        // 倒序查找最后一个有效的 SO，获取它的 ID
        for (int i = listProp.arraySize - 1; i >= 0; i--)
        {
            SerializedProperty elementProp = listProp.GetArrayElementAtIndex(i);
            if (elementProp.objectReferenceValue != null)
            {
                SerializedObject lastObj = new SerializedObject(elementProp.objectReferenceValue);
                SerializedProperty idProp = lastObj.FindProperty("id");
                if (idProp != null)
                {
                    newId = idProp.intValue + 1;
                    break;
                }
            }
        }

        // 如果列表为空，给一个默认初始 ID
        if (newId == 0)
        {
            if (currentTab == 0) newId = 2001;       // 卡牌默认从 2001 开始
            else if (currentTab == 1) newId = 1001;  // 角色默认从 1001 开始
            else newId = 1;                          // 关卡默认从 1 开始
        }

        // 2. 确定保存路径和文件名
        string subFolder = currentTab == 0 ? "Card" : (currentTab == 1 ? "Character" : "Level");
        string folderPath = $"{DATA_ROOT_PATH}/{subFolder}";

        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        // 使用新 ID 命名文件
        string fileName = $"{subFolder}_{newId}.asset";
        string fullPath = $"{folderPath}/{fileName}";

        // 3. 创建对应的 ScriptableObject 实例
        ScriptableObject newAsset = null;
        if (currentTab == 0) newAsset = CreateInstance<CardData>();
        else if (currentTab == 1) newAsset = CreateInstance<CharacterData>();
        else newAsset = CreateInstance<LevelData>();

        // 4. 将计算好的 newId 直接写入新创建的资产中
        SerializedObject newAssetObj = new SerializedObject(newAsset);
        SerializedProperty newIdProp = newAssetObj.FindProperty("id");
        if (newIdProp != null)
        {
            newIdProp.intValue = newId;
            newAssetObj.ApplyModifiedPropertiesWithoutUndo();
        }

        // 5. 在磁盘上生成 .asset 文件
        AssetDatabase.CreateAsset(newAsset, fullPath);

        // 6. 加到 GameConfigDatabase 的列表里
        listProp.arraySize++;
        SerializedProperty newElement = listProp.GetArrayElementAtIndex(listProp.arraySize - 1);
        newElement.objectReferenceValue = newAsset;

        // 7. 保存刷新
        serializedDatabase.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 选中刚创建的新数据
        selectedIndex = listProp.arraySize - 1;
        GUI.FocusControl(null);
    }

    private void DeleteCurrentData()
    {
        if (EditorUtility.DisplayDialog("危险操作", "确定要删除这条数据吗？对应的本地 .asset 文件也将被永久删除！", "确认删除", "取消"))
        {
            SerializedProperty listProp = GetCurrentListProperty();
            SerializedProperty elementProp = listProp.GetArrayElementAtIndex(selectedIndex);
            
            Object assetToDelete = elementProp.objectReferenceValue;

            // 从列表移除
            elementProp.objectReferenceValue = null;
            listProp.DeleteArrayElementAtIndex(selectedIndex);
            serializedDatabase.ApplyModifiedProperties();

            // 从磁盘彻底删除文件
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
        Debug.Log("数据保存成功！");
    }

    #endregion

    private void InitStyles()
    {
        if (sidebarStyle == null)
        {
            sidebarStyle = new GUIStyle("box");
            sidebarStyle.padding = new RectOffset(0, 0, 0, 0); 
        }
        if (normalBtnStyle == null)
        {
            normalBtnStyle = new GUIStyle(GUI.skin.button);
            normalBtnStyle.alignment = TextAnchor.MiddleLeft;
            normalBtnStyle.fixedHeight = 30;
            normalBtnStyle.margin = new RectOffset(2, 2, 2, 2);
        }
        if (selectedBtnStyle == null)
        {
            selectedBtnStyle = new GUIStyle(GUI.skin.button);
            selectedBtnStyle.alignment = TextAnchor.MiddleLeft;
            selectedBtnStyle.fixedHeight = 30;
            selectedBtnStyle.normal.textColor = new Color(0.2f, 0.8f, 1f); 
            selectedBtnStyle.fontStyle = FontStyle.Bold;
            selectedBtnStyle.margin = new RectOffset(2, 2, 2, 2);
        }
    }
}