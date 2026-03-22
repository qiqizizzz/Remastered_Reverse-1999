/*
* ┌──────────────────────────────────┐
* │  描    述: 提示框                      
* │  类    名: TipBoxView.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using DG.Tweening;
using MVC.View;
using TMPro;
using UnityEngine;

namespace Module.View
{
    public enum TipBoxType
    {
        chat, //聊天提示框
    }
    
    public class TipBoxView : BaseView
    {
        private RectTransform _rect;
        private TextMeshProUGUI _text;
        private Dictionary<string, GameObject> _imageList;
        private Transform iconsTf;
        private GameObject _currentIcon;
        private CanvasGroup _canvasGroup;

        [Header("动画参数")]
        [SerializeField] private Vector2 _targetPos = new Vector2(-30f, -120f); // 右上偏下
        [SerializeField] private float _enterDuration = 0.7f;
        [SerializeField] private float _staySeconds = 4f;
        [SerializeField] private float _exitDuration = 0.7f;
        [SerializeField] private float _offscreenX = 450f; // 屏幕右侧外偏移
        [SerializeField] private float _exitUpY = 120f;     // 退场上移距离
        
        private Tween _tween;

        protected override void OnAwake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            _rect = GetComponent<RectTransform>();
            _text = Find<TextMeshProUGUI>("Txt");
            iconsTf = Find<Transform>("Icons");
            _imageList = new Dictionary<string, GameObject>();
        }
        
        public override void Open(params object[] args)
        {
            //参数一：提示框类型
            //参数二：提示内容
            TipBoxType type = TipBoxType.chat;
            string content = "";

            if(_imageList.Count == 0) BuildImageList();
            
            if (args != null && args.Length > 0 && args[0] is TipBoxType)
                type = (TipBoxType)args[0];
            
            if(args != null && args.Length > 1 && args[1] != null)
                content = args[1].ToString();

            if (_text != null)
            {
                if(content == string.Empty) content = GetDefaultContent(type);
                else
                {
                    GetDefaultContent(type);
                    _text.text = content;
                }
            }
            
            PlayAnim();
        }
        
        private void BuildImageList()
        {
            _imageList.Clear();
            
            foreach (var child in iconsTf.GetComponentsInChildren<Transform>(true))
            {
                if (child == iconsTf) continue;

                _imageList[child.name] = child.gameObject;
                _imageList[child.name].SetActive(false);
            }
        }
        
        private string GetDefaultContent(TipBoxType type)
        {
            _currentIcon?.SetActive(false);//重置之前的图标状态
            string content = "";
            
            switch (type)
            {
                case TipBoxType.chat:
                    _currentIcon = _imageList["Icon_chat"];
                    content =  "无法发送空白消息";break;
                default:
                    content = "未知提示类型";break;
            }
            
            _currentIcon?.SetActive(true);
            return content;
        }

        private void PlayAnim()
        {
            _tween?.Kill();
            _tween = null;
            _canvasGroup.alpha = 1f;

            Vector2 startPos = _targetPos + new Vector2(_offscreenX, 0f);
            _rect.anchoredPosition = startPos;
            
            _tween = DOTween.Sequence()
                .SetUpdate(true) //忽略timeScale
                .Append(_rect.DOAnchorPos(_targetPos, _enterDuration).SetEase(Ease.OutCubic))//入场动画
                .AppendInterval(_staySeconds)//停留时间
                .Append(_rect.DOAnchorPos(_targetPos + new Vector2(0f, _exitUpY), _exitDuration).SetEase(Ease.InCubic))//退场动画
                .Join(_canvasGroup.DOFade(0f, _exitDuration))//退场淡出
                .OnComplete(() => //动画结束后重置状态并关闭视图
                {
                    _canvasGroup.alpha = 1f;
                    GameApp.ViewManager.Close(ViewId);
                });
        }
    }
}