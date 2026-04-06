/*
* ┌─────────────────────────────────────────────────────────┐
* │  描    述: 游戏数据编辑器窗口，支持编辑卡牌、角色和关卡配置                      
* │  类    名: GameDataEditorWindow.cs       
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────────┘
*/

using Data;
using UnityEditor;
using UnityEngine;

namespace DefaultNamespace.Editor
{
    public class GameDataEditorWindow : EditorWindow
{
    // 数据源
    private GameConfigDatabase database;
    private SerializedObject serializedDatabase;

    // 界面控制状态
    private int currentTab = 0; // 0:卡牌, 1:角色, 2:关卡
    private string[] tabNames = { "卡牌配置 (Cards)", "角色配置 (Characters)", "关卡配置 (Levels)" };
    
    private int selectedIndex = -1;
    private Vector2 leftScrollPos;
    private Vector2 rightScrollPos;

    // GUI 样式
    private GUIStyle sidebarStyle;
    private GUIStyle selectedBtnStyle;
    private GUIStyle normalBtnStyle;

    [MenuItem("Tools/游戏数据编辑器", priority = 0)]
    public static void ShowWindow()
    {
        // 打开窗口
        var window = GetWindow<GameDataEditorWindow>("数据编辑器");
        window.minSize = new Vector2(800, 500);
        window.Show();
    }

    private void OnEnable()
    {
        LoadDatabase();
    }

    // 尝试在工程里自动找到你的 GameConfigDatabase
    private void LoadDatabase()
    {
        string[] guids = AssetDatabase.FindAssets("t:GameConfigDatabase");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            database = AssetDatabase.LoadAssetAtPath<GameConfigDatabase>(path);
            if (database != null)
            {
                // 用 SerializedObject 包装，这非常重要！这能提供撤销(Ctrl+Z)和自动保存功能
                serializedDatabase = new SerializedObject(database);
            }
        }
    }

    private void OnGUI()
    {
        if (database == null || serializedDatabase == null)
        {
            EditorGUILayout.HelpBox("未能找到 GameConfigDatabase! 请确保你在工程里创建了它。", MessageType.Error);
            if (GUILayout.Button("重试加载")) LoadDatabase();
            return;
        }

        InitStyles();

        // 每次 OnGUI 开头都要 Update 一下数据
        serializedDatabase.Update();

        // 1. 绘制顶部页签
        DrawTopTabs();

        // 2. 绘制主体 (左右分栏)
        EditorGUILayout.BeginHorizontal();
        {
            DrawLeftSidebar();
            DrawRightPanel();
        }
        EditorGUILayout.EndHorizontal();

        // 每次 OnGUI 结尾都要 Apply 修改
        serializedDatabase.ApplyModifiedProperties();
    }

    #region UI 绘制逻辑

    private void DrawTopTabs()
    {
        GUILayout.Space(5);
        EditorGUI.BeginChangeCheck();
        currentTab = GUILayout.Toolbar(currentTab, tabNames, GUILayout.Height(30));
        if (EditorGUI.EndChangeCheck())
        {
            // 切换页签时，重置选中项
            selectedIndex = -1;
            GUI.FocusControl(null); 
        }
        GUILayout.Space(5);
    }

    private void DrawLeftSidebar()
    {
        // 锁定左侧宽度为 250
        EditorGUILayout.BeginVertical(sidebarStyle, GUILayout.Width(250), GUILayout.ExpandHeight(true));
        leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos);

        // 根据当前选中的页签，获取对应的列表属性
        string propertyName = currentTab == 0 ? "allCards" : (currentTab == 1 ? "allCharacters" : "allLevels");
        SerializedProperty listProperty = serializedDatabase.FindProperty(propertyName);

        // 遍历画出左侧的按钮
        for (int i = 0; i < listProperty.arraySize; i++)
        {
            SerializedProperty elementProp = listProperty.GetArrayElementAtIndex(i);
            
            // 尝试获取一个用来显示的名字 (如果有的话)
            string displayName = $"Item {i}";
            if (elementProp.objectReferenceValue != null)
            {
                displayName = elementProp.objectReferenceValue.name; // 用 SO 资产的文件名作为显示名
            }
            else
            {
                displayName = "空 (Null)";
            }

            // 判断是否是当前选中项，使用不同的样式
            GUIStyle btnStyle = (selectedIndex == i) ? selectedBtnStyle : normalBtnStyle;

            if (GUILayout.Button(displayName, btnStyle))
            {
                selectedIndex = i;
                GUI.FocusControl(null); // 取消输入框焦点，防止切数据时文字残留
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawRightPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos);

        string propertyName = currentTab == 0 ? "allCards" : (currentTab == 1 ? "allCharacters" : "allLevels");
        SerializedProperty listProperty = serializedDatabase.FindProperty(propertyName);

        if (selectedIndex >= 0 && selectedIndex < listProperty.arraySize)
        {
            SerializedProperty elementProp = listProperty.GetArrayElementAtIndex(selectedIndex);
            
            if (elementProp.objectReferenceValue != null)
            {
                // 获取当前选中 SO 的 SerializedObject
                SerializedObject itemObj = new SerializedObject(elementProp.objectReferenceValue);
                itemObj.Update();

                GUILayout.Label("详细配置", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                // 遍历并绘制这个 SO 里面的所有属性！
                SerializedProperty iterator = itemObj.GetIterator();
                bool enterChildren = true;
                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (iterator.name == "m_Script") continue; // 隐藏默认的 Script 引用字段

                    EditorGUILayout.PropertyField(iterator, true);
                }

                itemObj.ApplyModifiedProperties();
            }
            else
            {
                EditorGUILayout.HelpBox("该槽位为空，请在 GameConfigDatabase 中拖入对应的 ScriptableObject 文件！", MessageType.Warning);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("请在左侧选择一个配置项查看详情。", MessageType.Info);
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    // 初始化一些简单的颜色和样式
    private void InitStyles()
    {
        if (sidebarStyle == null)
        {
            sidebarStyle = new GUIStyle("box");
            sidebarStyle.padding = new RectOffset(5, 5, 5, 5);
        }
        if (normalBtnStyle == null)
        {
            normalBtnStyle = new GUIStyle(GUI.skin.button);
            normalBtnStyle.alignment = TextAnchor.MiddleLeft;
            normalBtnStyle.fixedHeight = 25;
        }
        if (selectedBtnStyle == null)
        {
            selectedBtnStyle = new GUIStyle(GUI.skin.button);
            selectedBtnStyle.alignment = TextAnchor.MiddleLeft;
            selectedBtnStyle.fixedHeight = 25;
            selectedBtnStyle.normal.textColor = Color.cyan; // 选中时文字变青色
            selectedBtnStyle.fontStyle = FontStyle.Bold;
        }
    }

    #endregion
}
}