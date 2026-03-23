/*
* ┌──────────────────────────────────┐
* │  描    述: 提示界面                      
* │  类    名: NoticeView.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using MVC.View;
using TMPro;
using UnityEngine.UI;

namespace Module.View
{
    public class NoticeView : BaseView
    {
        private TextMeshProUGUI _txtContent;
        
        private Action _onConfirmCallback;
        private Action _onCancelCallback;
        
        protected override void OnAwake()
        {
            _txtContent = Find<TextMeshProUGUI>("Img_txtBg/Txt");
            Find<Button>("Btn_cancel").onClick.AddListener(onCancelBtn);
            Find<Button>("Btn_confirm").onClick.AddListener(onConfirmBtn);
        }

        public override void Open(params object[] args)
        {
            // args[0]：提示内容
            // args[1]：确认按钮的回调 (Action)
            // args[2]：取消按钮的回调 (Action)
            
            string content = args.Length > 0 ? args[0] as string : "这是一个提示界面";
            _txtContent.text = content;
            
            // 每次打开弹窗时，重新接收并覆盖回调函数
            _onConfirmCallback = args.Length > 1 ? args[1] as Action : null;
            _onCancelCallback = args.Length > 2 ? args[2] as Action : null;
        }

        private void onCancelBtn()
        {
            _onCancelCallback?.Invoke();
            GameApp.ViewManager.Close(ViewId);
        }

        private void onConfirmBtn()
        {
            _onConfirmCallback?.Invoke();
            GameApp.ViewManager.Close(ViewId);
        }
    }
}