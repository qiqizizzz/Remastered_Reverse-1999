/*
* ┌──────────────────────────────────┐
* │  描    述: 选择角色UI                      
* │  类    名: CharacterSelectItem.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Data.card;
using TMPro;
using UnityEngine;

namespace Module.level.Component
{
    public class CharacterSelectItem : MonoBehaviour
    {
        private int startIndex = 1001;
        private TextMeshProUGUI txt;

        private void Awake()
        {
            txt = GetComponentInChildren<TextMeshProUGUI>();
        }

        // 方法名必须和 SendMessage 里的名字一模一样！
        void ScrollCellIndex(int idx)
        {
            // idx 就是当前卡片的索引（0代表第1个，1代表第2个...）
        
            // 测试：把节点名字改成对应的索引，方便你在 Hierarchy 里观察
            CharacterData data = GameApp.ConfigManager.GetCharacterData(startIndex + idx);
            
            gameObject.name = "Card_" + data.id;
            txt.text = data.name;


            // 在这里写你的业务逻辑，比如：
            // 1. 根据 idx 从你的数据列表里拿到角色数据 (苏芙比、百夫长等)
            // 2. 替换卡片上的头像图片
            // 3. 替换卡片上的名字 Text
        }
    }
}