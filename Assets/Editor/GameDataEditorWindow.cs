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
using Data.level;

public class GameDataEditorWindow : EditorWindow
{
    private GameConfigDatabase database;
    private SerializedObject serializedDatabase;

    // 分类页签状态
    private int mainTab = 0; 
    private int cardSubTab = 0;
    private int entitySubTab = 0;

    private readonly string[] mainTabNames = { "💳 卡牌管理", "🦸 实体管理", "🗺️ 关卡配置" };
    private readonly string[] cardSubTabNames = { "🗡️ 玩家卡牌", "👿 敌人卡牌" };
    private readonly string[] entitySubTabNames = { "🦸 玩家角色", "👿 敌人实体" };
    
    private int selectedIndex = -1;
    private Vector2 leftScrollPos;
    private Vector2 rightScrollPos;

    // 自定义 GUI 样式
    private GUIStyle sidebarStyle;
    private GUIStyle selectedBtnStyle;
    private GUIStyle normalBtnStyle;
    private GUIStyle headerStyle;
    private GUIStyle panelStyle;

    private readonly string DATA_ROOT_PATH = "Assets/RemoteAssets/Data";

    [MenuItem("Tools/游戏数据编辑器", priority = 0)]
    public static void ShowWindow()
    {
        var window = GetWindow<GameDataEditorWindow>("数据编辑器");
        window.minSize = new Vector2(900, 600);
        window.Show();
    }

    private void OnEnable() => LoadDatabase();

    private void LoadDatabase()
    {
        string[] guids = AssetDatabase.FindAssets("t:GameConfigDatabase");
        if (guids.Length > 0)
        {
            database = AssetDatabase.LoadAssetAtPath<GameConfigDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
            if (database != null) serializedDatabase = new SerializedObject(database);
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
    
        // 使用水平布局，将所有按钮排列靠左对齐
        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
    
        GUI.backgroundColor = new Color(0.9f, 0.9f, 0.9f);
        mainTab = GUILayout.Toolbar(mainTab, mainTabNames, GUILayout.Height(30), GUILayout.Width(400));
        GUI.backgroundColor = Color.white;

        if (EditorGUI.EndChangeCheck())
        {
            selectedIndex = -1;
            GUI.FocusControl(null);
        }

        GUILayout.FlexibleSpace(); // 添加弹簧，确保按钮不占满整个窗口
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(5);

        EditorGUILayout.EndVertical();
    }

    private void DrawLeftSidebar()
    {
        EditorGUILayout.BeginVertical(sidebarStyle, GUILayout.Width(260), GUILayout.ExpandHeight(true));
        
        // 下拉列表作为子分类选择
        if (mainTab == 0 || mainTab == 1)
        {
            EditorGUI.BeginChangeCheck();
            if (mainTab == 0)
                cardSubTab = EditorGUILayout.Popup(cardSubTab, cardSubTabNames, EditorStyles.popup);
            else if (mainTab == 1)
                entitySubTab = EditorGUILayout.Popup(entitySubTab, entitySubTabNames, EditorStyles.popup);
                
            if (EditorGUI.EndChangeCheck())
            {
                selectedIndex = -1;
                GUI.FocusControl(null);
            }
            GUILayout.Space(5);
        }

        SerializedProperty listProperty = GetCurrentListProperty();

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
            GUIUtility.ExitGUI();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        if (listProperty == null)
        {
            EditorGUILayout.HelpBox("未找到对应的数据列表，请检查 GameConfigDatabase 中的变量命名是否正确。", MessageType.Warning);
            EditorGUILayout.EndVertical();
            return;
        }

        leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos);
        for (int i = 0; i < listProperty.arraySize; i++)
        {
            SerializedProperty elementProp = listProperty.GetArrayElementAtIndex(i);
            bool isNull = elementProp.objectReferenceValue == null;
            
            Rect rowRect = EditorGUILayout.BeginHorizontal();
            if (i % 2 == 0) EditorGUI.DrawRect(rowRect, new Color(0, 0, 0, 0.05f));
            if (selectedIndex == i) EditorGUI.DrawRect(rowRect, new Color(0.15f, 0.45f, 0.8f, 0.2f));

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
        EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f, 0.5f)); 
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
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                GUILayout.Label($" 📝 当前编辑:  {elementProp.objectReferenceValue.name}", headerStyle);
                GUILayout.FlexibleSpace(); 
                
                if (GUILayout.Button("🔍 定位", EditorStyles.toolbarButton, GUILayout.Width(50)))
                    EditorGUIUtility.PingObject(elementProp.objectReferenceValue); 
                
                GUI.backgroundColor = new Color(0.6f, 0.8f, 1f);
                if (GUILayout.Button("💾 保存", EditorStyles.toolbarButton, GUILayout.Width(50)))
                {
                    AssetDatabase.SaveAssets();
                    Debug.Log("<color=green>数据保存成功！</color>");
                }
                
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f); 
                if (GUILayout.Button("🗑️ 删除", EditorStyles.toolbarButton, GUILayout.Width(50)))
                    DeleteCurrentData(listProperty, elementProp.objectReferenceValue);
                    
                GUI.backgroundColor = Color.white; 
                EditorGUILayout.EndHorizontal();

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
            else DrawMissingDataWarning(listProperty);
        }
        else
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label("👈 请在左侧选择或新建一个配置项", EditorStyles.largeLabel, GUILayout.ExpandWidth(false));
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

    #region 数据配置映射与逻辑

    private void GetCurrentContext(out string propName, out string folder, out System.Type type, out int defaultId)
    {
        if (mainTab == 0)
        {
            type = typeof(CardData);
            if (cardSubTab == 0) { propName = "allCards"; folder = "CharacterCard"; defaultId = 2001; }
            else                 { propName = "allEnemyCards"; folder = "EnemyCard"; defaultId = 3001; }
        }
        else if (mainTab == 1)
        {
            type = typeof(CharacterData);
            if (entitySubTab == 0) { propName = "allCharacters"; folder = "Character"; defaultId = 1001; }
            else                   { propName = "allEnemies"; folder = "Enemy"; defaultId = 5001; }
        }
        else
        {
            type = typeof(LevelData);
            propName = "allLevels"; folder = "Level"; defaultId = 1;
        }
    }

    private SerializedProperty GetCurrentListProperty()
    {
        GetCurrentContext(out string propName, out _, out _, out _);
        return serializedDatabase.FindProperty(propName);
    }

    private void CleanNullEntries(SerializedProperty listProp)
    {
        if (listProp == null) return;
        for (int i = listProp.arraySize - 1; i >= 0; i--)
            if (listProp.GetArrayElementAtIndex(i).objectReferenceValue == null)
                listProp.DeleteArrayElementAtIndex(i);
        
        serializedDatabase.ApplyModifiedProperties();
        selectedIndex = -1;
    }

    private void CreateNewData()
    {
        SerializedProperty listProp = GetCurrentListProperty();
        if (listProp == null) return;
        
        GetCurrentContext(out _, out string folder, out System.Type type, out int newId);
        
        for (int i = listProp.arraySize - 1; i >= 0; i--)
        {
            var element = listProp.GetArrayElementAtIndex(i).objectReferenceValue;
            if (element != null)
            {
                SerializedProperty idProp = new SerializedObject(element).FindProperty("Id"); 
                if (idProp != null) { newId = idProp.intValue + 1; break; }
            }
        }

        string folderPath = $"{DATA_ROOT_PATH}/{folder}";
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        string fullPath = $"{folderPath}/{folder}_{newId}.asset";

        ScriptableObject newAsset = CreateInstance(type);
        AssetDatabase.CreateAsset(newAsset, fullPath);

        SerializedObject newAssetObj = new SerializedObject(newAsset);
        SerializedProperty newIdProp = newAssetObj.FindProperty("Id"); 
        if (newIdProp != null)
        {
            newIdProp.intValue = newId;
            newAssetObj.ApplyModifiedPropertiesWithoutUndo();
        }

        listProp.arraySize++;
        listProp.GetArrayElementAtIndex(listProp.arraySize - 1).objectReferenceValue = newAsset;

        serializedDatabase.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();

        selectedIndex = listProp.arraySize - 1;
        GUI.FocusControl(null);
    }

    private void DeleteCurrentData(SerializedProperty listProp, Object assetToDelete)
    {
        if (EditorUtility.DisplayDialog("⚠️ 危险操作", "确定要删除这条数据吗？本地 .asset 文件将被永久删除！", "确认", "取消"))
        {
            listProp.GetArrayElementAtIndex(selectedIndex).objectReferenceValue = null;
            listProp.DeleteArrayElementAtIndex(selectedIndex);
            serializedDatabase.ApplyModifiedProperties();

            if (assetToDelete != null)
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(assetToDelete));

            AssetDatabase.SaveAssets();
            selectedIndex = -1;
            GUIUtility.ExitGUI();
        }
    }

    #endregion

    #region 美化样式初始化
    private void InitStyles()
    {
        if (sidebarStyle == null)
            sidebarStyle = new GUIStyle("box") { padding = new RectOffset(5, 5, 5, 5), margin = new RectOffset(0, 0, 0, 0) };
        if (panelStyle == null)
            panelStyle = new GUIStyle() { padding = new RectOffset(5, 5, 5, 5) };
        if (normalBtnStyle == null)
            normalBtnStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft, fixedHeight = 32, margin = new RectOffset(2, 2, 2, 2) };
        if (selectedBtnStyle == null)
        {
            selectedBtnStyle = new GUIStyle(GUI.skin.button) 
            { 
                alignment = TextAnchor.MiddleLeft, fixedHeight = 32, margin = new RectOffset(2, 2, 2, 2), fontStyle = FontStyle.Bold 
            };
            selectedBtnStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.4f, 0.8f, 1f) : new Color(0.1f, 0.4f, 0.8f);
        }
        if (headerStyle == null)
            headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13, alignment = TextAnchor.MiddleLeft };
    }
    #endregion
}