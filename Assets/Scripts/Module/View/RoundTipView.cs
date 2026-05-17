/*
* ┌──────────────────────────────────┐
* │  描    述: 回合UI                      
* │  类    名: UI_ActionRound.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using DG.Tweening;
using MVC.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Module.View
{
    public class RoundTipView : BaseView
    {
        [Header("UI References")]
        public TextMeshProUGUI textRound;
        public Image content;

        protected override void OnAwake()
        {
            textRound = Find<TextMeshProUGUI>("content/Txt_Round");
            content = Find<Image>("content");
        }

        public override void Open(params object[] args)
        {
            textRound.text = args[0] as string;

            Action onComplete = null;
            if (args.Length > 1 && args[^1] is Action callback)
                onComplete = callback;

            content.transform.localScale = new Vector3(1, 0, 1);

            Sequence seq = DOTween.Sequence();
            seq.Append(content.transform.DOScaleY(1, 0.15f).SetEase(Ease.OutBack));
            seq.AppendInterval(0.75f);
            seq.Append(content.transform.DOScaleY(0, 0.15f).SetEase(Ease.Linear));
            seq.AppendCallback(delegate()
            {
                onComplete?.Invoke();
                GameApp.ViewManager.Close(ViewId);
            });
        }
    }
}