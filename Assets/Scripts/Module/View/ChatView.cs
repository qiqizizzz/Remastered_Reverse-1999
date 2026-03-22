/*
* ┌──────────────────────────────────┐
* │  描    述: 好友界面                      
* │  类    名: ChatView.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections;
using System.Collections.Generic;
using Common;
using Common.Defines;
using DG.Tweening;
using GameProtocol;
using Module.chat;
using MVC;
using MVC.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Module.View
{
    public class ChatView : BaseView
    {
        private Button _currentChat;//当前聊天对象
        private const int friendMaxCount = 50;//好友上限
        
        [Header("打字效果相关")]
        private List<Vector3> _charPositions = new List<Vector3>(); // 缓存每个字符的位置
        private string lastText="";
        
        [Header("UI组件")]
        private TMP_InputField _inputField;
        private ScrollRect _chatScrollRect;
        private ScrollRect _friendScrollRect;
        private Transform _chatPanel;
        
        protected override void OnAwake()
        {
            _inputField = Find<TMP_InputField>("panels/panel_friend/chatArea/Input_field");
            _chatScrollRect = Find<ScrollRect>("panels/panel_friend/chatArea/Scroll_chat");
            _friendScrollRect = Find<ScrollRect>("panels/panel_friend/Scroll_hy");
            _chatPanel = Find<Transform>("panels/panel_friend");
            
            Find<Button>("Btn_return").onClick.AddListener(onReturnMoreOptionBtn);
            Find<Button>("panels/panel_friend/chatArea/Btn_send").onClick.AddListener(onSendMessageBtn);
        }

        protected override void OnStart()
        {
            _inputField.onValueChanged.AddListener(onInputFieldValueChanged);
            
            Controller.RegisterFunc(EventDefines.UpdateFriendList, onUpdateFriendList);
            Controller.RegisterFunc(EventDefines.UpdateChatHistory, onUpdateChatHistory);
            Controller.RegisterFunc(EventDefines.ReceiveNewMessage, onReceiveNewMessage);
            
            ApplyFunc(EventDefines.GetFriendList);//发送获取好友列表请求
        }

        protected override void OnDestroy()
        {
            Controller.UnRegisterFunc(EventDefines.UpdateFriendList);
            Controller.UnRegisterFunc(EventDefines.GetChatHistory);
            Controller.UnRegisterFunc(EventDefines.ReceiveNewMessage);
        }

        private void onReturnMoreOptionBtn()
        {
            GameApp.ViewManager.Close(ViewId);
        }

        private void onFriendBtn(string friendName, GameObject btnObj)
        {
            Find<TextMeshProUGUI>("panels/panel_friend/chatArea/Txt_name").text = friendName;
            
            //取消之前的选中状态
            if(_currentChat != null)
                _currentChat.transform.Find("Imgs_bg/Img_selected").gameObject.SetActive(false);
            
            btnObj.transform.Find("Imgs_bg/Img_selected").gameObject.SetActive(true);//设置当前按钮的选中状态
            
            //如果点击的好友已经是当前聊天对象，则不重复加载聊天记录
            if(_currentChat == btnObj.GetComponent<Button>())
                return;
            
            _currentChat = btnObj.GetComponent<Button>();
            ApplyFunc(EventDefines.GetChatHistory, friendName);
        }

        private void onSendMessageBtn()
        {
            string content = _inputField.text;
            string targetUser = Find<TextMeshProUGUI>("panels/panel_friend/chatArea/Txt_name").text;
            Transform contentParent = Find<Transform>("panels/panel_friend/chatArea/Scroll_chat/Viewport/Content");

            if (content == string.Empty)
            {
                Debug.Log("输入不能为空");
                ApplyControllerFunc(ControllerType.GameUI, EventDefines.OpenTipBoxView, TipBoxType.chat, "输入不能为空");
                return;
            }
            
            ResManager.InstantiateFromPoolAsync(AddressDefines.UI_Small_chatBox_me, (go) =>
            {
                if (go != null)
                {
                    go.GetComponent<ChatBubble>().SetMessage(content);
                    _inputField.text = string.Empty;
                    StartCoroutine(scrollToDown());
                }
            }, contentParent);
            
            //保存至数据库
            ApplyFunc(EventDefines.SendPrivateMessage, targetUser, content);
        }

        private IEnumerator scrollToDown()
        {
            yield return new WaitForEndOfFrame();
            
            if (_chatScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();//强制刷新
                _chatScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        #region 打字效果
        private void onInputFieldValueChanged(string value)
        {
            int lastLength = lastText.Length;
            int currentLength = value.Length;
            
            if (currentLength > lastLength) //输入内容增加
            {
                string added = value.Substring(lastLength);
                for (int i = 0; i < added.Length; i++)
                {
                    AddCharEffect(added[i], lastLength + i);
                }
            }
            else if(currentLength < lastLength) //输入内容减少
            {
                int removeCount = lastLength - currentLength;
                string removedStr = lastText.Substring(currentLength, removeCount);
                
                for (int i = removedStr.Length - 1; i >= 0; i--)
                {
                    int oldIndex = currentLength + i;
                    Vector3 dropPos = Vector3.zero;
                    
                    // 从缓存中获取被删字符的原本位置
                    if (oldIndex < _charPositions.Count)
                        dropPos = _charPositions[oldIndex];
                    else if (_charPositions.Count > 0)
                        dropPos = _charPositions[_charPositions.Count - 1]; // 降级保护

                    RemoveCharEffect(removedStr[i], dropPos);
                }
            }
            
            lastText = value;
            
            StartCoroutine(UpdateCharPositions());//更新并缓存当前输入框内所有字符的坐标
        }

        // 更新并缓存当前输入框内所有字符的坐标
        private IEnumerator UpdateCharPositions()
        {
            yield return new WaitForEndOfFrame();
            if (_inputField == null) yield break;
            
            TMP_Text textComponent = _inputField.textComponent;
            textComponent.ForceMeshUpdate();
            TMP_TextInfo textInfo = textComponent.textInfo;

            _charPositions.Clear();
            for (int i = 0; i < textInfo.characterCount; i++)
            {
                // 获取字符左下角位置作为生成特效的基准点
                Vector3 localPos = textInfo.characterInfo[i].bottomLeft;
                Vector3 worldPos = textComponent.transform.TransformPoint(localPos);
                _charPositions.Add(worldPos);
            }
        }

        // 输入内容增加时, 慢慢浮现特效
        private void AddCharEffect(char c, int charIndex)
        {
            if (c == ' ') return;

            TMP_Text textComponent = _inputField.textComponent;

            DOVirtual.Float(0f, 1f, 0.25f, (alpha) =>
            {
                if (_inputField == null || textComponent == null) return;
                
                // 强制更新网格以获取最新顶点数据
                textComponent.ForceMeshUpdate();
                TMP_TextInfo textInfo = textComponent.textInfo;

                // 越界和可见性保护
                if (charIndex >= textInfo.characterCount) return;
                if (!textInfo.characterInfo[charIndex].isVisible) return;

                // 获取当前字符的顶点颜色数组
                int materialIndex = textInfo.characterInfo[charIndex].materialReferenceIndex;
                int vertexIndex = textInfo.characterInfo[charIndex].vertexIndex;
                Color32[] vertexColors = textInfo.meshInfo[materialIndex].colors32;

                // 修改该字符 4 个顶点的透明度
                byte byteAlpha = (byte)(alpha * 255);
                vertexColors[vertexIndex + 0].a = byteAlpha;
                vertexColors[vertexIndex + 1].a = byteAlpha;
                vertexColors[vertexIndex + 2].a = byteAlpha;
                vertexColors[vertexIndex + 3].a = byteAlpha;

                // 将修改后的颜色更新到 Mesh 上
                textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            });
        }

        // 输入内容删除时,这个字往下掉落并渐渐消失
        private void RemoveCharEffect(char c, Vector3 dropPos)
        {
            if (c == ' ') return;

            // 动态生成掉落文字
            GameObject dropObj = new GameObject("DropChar_" + c);
            dropObj.transform.SetParent(_inputField.transform, false);
            
            TextMeshProUGUI dropText = dropObj.AddComponent<TextMeshProUGUI>();
            TMP_Text originalText = _inputField.textComponent;

            dropText.text = c.ToString();
            dropText.font = originalText.font;
            dropText.fontSize = originalText.fontSize;
            dropText.color = originalText.color;
            dropText.raycastTarget = false;

            dropText.rectTransform.pivot = new Vector2(0f, 0f);
            dropText.rectTransform.position = dropPos;

            float rightOffset = originalText.fontSize * 0.4f; // 往右偏字体的40%
            float downOffset = -25f; // 往下偏25个像素
            dropText.rectTransform.anchoredPosition += new Vector2(rightOffset, downOffset);

            // 掉落动画 (相对当前位置Y轴再下移30像素，使用 InQuad 表现出重力加速掉落的感觉)
            float targetY = dropText.rectTransform.anchoredPosition.y - 30f;
            dropText.rectTransform.DOAnchorPosY(targetY, 0.3f).SetEase(Ease.InQuad);
            dropText.DOFade(0f, 0.3f).SetEase(Ease.OutQuad).OnComplete(() => Destroy(dropObj));
        }
        #endregion
        
        private void onUpdateFriendList(params object[] args)
        {
            Transform contentParent = _friendScrollRect.content; 
            List<FriendInfo> friends = (Controller as ChattingController)?.Model.FriendList;

            if (friends == null || friends.Count == 0)
            {
                //回收先前的好友列表UI
                foreach (Transform child in contentParent)
                    ResManager.ReleaseToPool(AddressDefines.UI_Small_Btn_friTemp, child.gameObject);
                Find<TextMeshProUGUI>("panels/panel_friend/Txt_num").text = $"0/{friendMaxCount}";
                
                //TODO:显示暂无好友提示
                return;
            }
            
            Find<TextMeshProUGUI>("panels/panel_friend/chatArea/Txt_name").text = friends[0].Username;
            //更新好友数量
            Find<TextMeshProUGUI>("panels/panel_friend/Txt_num").text = $"{friends.Count}/{friendMaxCount}";
            
            foreach (var item in friends)
            {
                string targetName = item.Username;
                
                ResManager.InstantiateFromPoolAsync(AddressDefines.UI_Small_Btn_friTemp,(go =>
                {
                    if (go != null)
                    {
                        go.transform.Find("Txt_name").GetComponent<TextMeshProUGUI>().text = targetName;
                        
                        //获取状态节点
                        TextMeshProUGUI txtTime = go.transform.Find("Txt_loginTime").GetComponent<TextMeshProUGUI>();
                        txtTime.text = item.IsOnline ? "<color=green>在线</color>" : "<color=#716860>离线</color>";
                        
                        Button btn = go.GetComponent<Button>();
                        btn.name = item.Username;
                        btn.onClick.RemoveAllListeners();//先移除之前的监听，避免重复绑定
                        btn.onClick.AddListener(() => onFriendBtn(targetName ,go));
                    }
                }),contentParent);
            }
        }

        private void onUpdateChatHistory(params object[] args)
        {
            //args[0] 是聊天对象的名字
            string targetUser = args[0] as string;
            if(targetUser != _currentChat.name) return;
            
            Transform contentParent = _chatScrollRect.content;
            List<ChatMessage> messages =
                (Controller as ChattingController)?.Model.ChatHistory.GetValueOrDefault(targetUser) ??
                new List<ChatMessage>();
            if(messages.Count == 0) return;
            
            //回收先前的聊天记录UI
            for (int i = contentParent.childCount - 1; i >= 0; i--)
            {
                Transform child = contentParent.GetChild(i);
                ResManager.ReleaseToPool(
                    child.name.Contains("me")
                        ? AddressDefines.UI_Small_chatBox_me
                        : AddressDefines.UI_Small_chatBox_other, child.gameObject);
            }
            
            //注意：不能直接使用 foreach 循环来回收，因为在回收过程中会修改 contentParent 的子对象集合，导致枚举器失效
            //会造成层级容易报错或者清理不干净,导致后续生成的聊天气泡层级混乱或者残留旧消息
            /*foreach (Transform child in contentParent)
            {
                ResManager.ReleaseToPool(AddressDefines.UI_Small_chatBox_me, child.gameObject);
                ResManager.ReleaseToPool(AddressDefines.UI_Small_chatBox_other, child.gameObject);
            }*/
            
            int loadedCount = 0;
            for(int i = 0; i < messages.Count; i++)
            {
                int index = i;//注意：在异步回调中使用循环变量时，必须创建一个局部副本，否则会导致所有回调都引用同一个变量，最终结果都是最后一个值
                
                var msg = messages[i];
                string prefabAddress = msg.IsSelf ? AddressDefines.UI_Small_chatBox_me : AddressDefines.UI_Small_chatBox_other;
                string content = msg.Content;
                
                ResManager.InstantiateFromPoolAsync(prefabAddress, (go) =>
                {
                    if (go != null)
                    {
                        go.GetComponent<ChatBubble>().SetMessage(content);
                        go.transform.SetSiblingIndex(index);//强制按索引排序,防止异步的优先生成资源加载快的预制体气泡
                        loadedCount++;
                    }

                    if (loadedCount == messages.Count)
                    {
                        StartCoroutine(scrollToDown());
                    }
                }, contentParent);
            }
        }

        private void onReceiveNewMessage(params object[] args)
        {
            string sender = args[0] as string;
            ChatMessage msg = args[1] as ChatMessage;
            
            if(msg == null) return;
            if(sender != _currentChat.name) return;
            
            Transform contentParent = Find<Transform>("panels/panel_friend/chatArea/Scroll_chat/Viewport/Content");
            
            string prefabAddress = msg.IsSelf ? AddressDefines.UI_Small_chatBox_me : AddressDefines.UI_Small_chatBox_other;

            //实例化新的消息气泡
            ResManager.InstantiateFromPoolAsync(prefabAddress, (go) =>
            {
                if (go != null)
                {
                    go.GetComponent<ChatBubble>().SetMessage(msg.Content);
                    StartCoroutine(scrollToDown()); // 消息追加后滚动到最底部
                }
            }, contentParent);
        }
    }
}