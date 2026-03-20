/*
* ┌──────────────────────────────────┐
* │  描    述: 聊天气泡                      
* │  类    名: ChatBubble.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using TMPro;
using UnityEngine;

namespace Module.chat
{
    public class ChatBubble : MonoBehaviour
    {
        private TextMeshProUGUI chatText;
        private RectTransform bgRect;
        private RectTransform rootRect;

        private float maxWidth = 450f;
        public Vector2 padding = new Vector2(40, 30);// 气泡内边距
        
        [SerializeField] private string Content = "";
        
        private void Awake()
        {
            chatText = GetComponentInChildren<TextMeshProUGUI>();
            bgRect = transform.Find("inputBox").GetComponent<RectTransform>();
            rootRect = GetComponent<RectTransform>();
        }

        private void Update()
        {
            SetMessage(Content);
        }

        /// <summary>
        /// 外部调用：设置聊天气泡的内容并自动排版
        /// </summary>
        public void SetMessage(string content)
        {
            chatText.text = content;

            //获取最大宽度
            chatText.rectTransform.sizeDelta = new Vector2(2000f, 2000f);
            chatText.ForceMeshUpdate();
            float rawWidth = chatText.preferredWidth;

            float finalWidth = Mathf.Min(rawWidth, maxWidth);

            //获取最大高度
            chatText.rectTransform.sizeDelta = new Vector2(finalWidth, 2000f);
            chatText.ForceMeshUpdate(); // 强迫TMP重新排版换行
            float finalHeight = chatText.preferredHeight;

            chatText.rectTransform.sizeDelta = new Vector2(finalWidth, finalHeight);

            float bgWidth = finalWidth + padding.x;
            float bgHeight = finalHeight + padding.y;
            bgRect.sizeDelta = new Vector2(bgWidth, bgHeight);

            rootRect.sizeDelta = new Vector2(rootRect.sizeDelta.x, bgHeight + 20f);
        }
    }
}