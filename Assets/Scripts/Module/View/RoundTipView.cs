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

namespace Module.Effect
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
            
            Sequence seq = DOTween.Sequence();
            seq.Append(content.transform.DOScaleY(1,0.15f).SetEase(Ease.OutBack));
            seq.AppendInterval(0.75f);
            seq.Append(content.transform.DOScaleY(0, 0.15f).SetEase(Ease.Linear));
            seq.AppendCallback(delegate()
            {
                GameApp.ViewManager.Close(ViewId);
            });
        }
    }
}