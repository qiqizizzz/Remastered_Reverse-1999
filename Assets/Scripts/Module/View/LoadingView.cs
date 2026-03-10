/*
* ┌──────────────────────────────────┐
* │  描    述: 加载界面                      
* │  类    名: LoadingView.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections;
using System.Collections.Generic;
using Common;
using Common.Defines;
using Data;
using DG.Tweening;
using Module.Loading;
using MVC;
using MVC.Extensions;
using MVC.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

namespace Module.View
{
    public class LoadingView : BaseView
    {
        //UI组件
        private Image bg;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI detailText;
        private Transform fillTf;
        
        //进度条处理
        private AsyncOperation _op;
        private float startPosX;
        private float _process;
        private float time = 0.2f;//进度条动画时间

        protected override void OnAwake()
        {
            bg = Find<Image>("bg");
            titleText = Find<TextMeshProUGUI>("title");
            detailText = Find<TextMeshProUGUI>("detail");
            fillTf = Find<Transform>("processBar/FillArea");
        }

        protected override void OnStart()
        {
            //加载随机提示数据
            ResManager.LoadAssetAsync<LoadingTextListSO>(AddressDefines.Data_LoadingTextData, data =>
                SetLoadingText(data.GetRandomTip()));
            
            startPosX = fillTf.localPosition.x;
        }

        protected override void OnUpdate()
        {
            UpdateFillTf();
        }

        public void InitLoading(AsyncOperation op) => _op = op;
        
        private void SetLoadingText(LoadingText text)
        {
            bg.sprite = text.bg;
            titleText.text = text.title;
            detailText.text = text.detail;
        }
        
        //更新进度条动画
        private void UpdateFillTf()
        {
            if(_op == null) return;
            
            float target = Mathf.Clamp01(_op.progress / 0.9f);//Unity的异步加载进度在0.9时会停下来，等待场景激活
            DOTween.To(() => _process, x => _process = x, target, time)
                .OnUpdate(() =>
                {
                    float posX = startPosX + (_process * 1920f);
                    fillTf.localPosition = new Vector3(posX, 0, 0);
                });

            //进度条接近完成后允许跳转
            if (_process >= 0.99f) _op.allowSceneActivation = true;
        }
    }
}