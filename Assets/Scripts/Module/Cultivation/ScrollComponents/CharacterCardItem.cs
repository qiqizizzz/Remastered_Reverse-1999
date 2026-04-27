/*
* ┌──────────────────────────────────┐
* │  描    述: 角色卡片                      
* │  类    名: CharacterCardItem.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using Data.card;
using TMPro;
using UnityEngine;

namespace Module.Cultivation.ScrollComponents
{
    public class CharacterCardItem : MonoBehaviour
    {
        private int startIndex = 1001;
        private TextMeshProUGUI txt;

        private void Awake()
        {
            txt = GetComponentInChildren<TextMeshProUGUI>();
        }

        void ScrollCellIndex(int idx)
        {
            CharacterDataSO data = GameApp.ConfigManager.GetCharacterData(startIndex + idx);
            
            gameObject.name = "Card_" + data.Id;
            txt.text = data.Name;
        }
    }
}