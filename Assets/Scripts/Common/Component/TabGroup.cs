/*
* ┌──────────────────────────────────┐
* │  描    述: 通用 Tab 切换组件，管理按钮与面板的显示切换
* │  类    名: TabGroup.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using UnityEngine;
using UnityEngine.UI;

namespace Common.Component
{
    [Serializable]
    public struct TabEntry
    {
        public Button TabButton;
        public GameObject Panel;
    }

    public class TabGroup : MonoBehaviour
    {
        [SerializeField] private TabEntry[] Tabs;

        private int _currentIndex = -1;

        public int CurrentIndex => _currentIndex;
        public Button CurrentButton => _currentIndex >= 0 && _currentIndex < Tabs.Length ? Tabs[_currentIndex].TabButton : null;

        public event Action<int> OnTabChanged;

        public void SetTabs(TabEntry[] tabs)
        {
            Tabs = tabs;
        }

        public void Init()
        {
            for (int i = 0; i < Tabs.Length; i++)
            {
                int index = i;
                Tabs[i].TabButton.onClick.AddListener(() => ShowTab(index));
            }

            if (Tabs.Length > 0)
                ShowTab(0);
        }

        private void ShowTab(int index)
        {
            if (index < 0 || index >= Tabs.Length) return;

            for (int i = 0; i < Tabs.Length; i++)
                Tabs[i].Panel.SetActive(i == index);

            _currentIndex = index;
            OnTabChanged?.Invoke(index);
        }
    }
}
