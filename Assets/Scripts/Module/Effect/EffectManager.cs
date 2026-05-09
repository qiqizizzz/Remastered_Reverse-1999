/*
* ┌──────────────────────────────────┐
* │  描    述: 效果管理器，全局鼠标点击涟漪特效
* │  类    名: EffectManager.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common;
using Module.Effect;
using UnityEngine;

namespace Module.Effect
{
    public class EffectManager
    {
        private readonly Transform _canvasTf;
        private readonly string _effectKey;

        public EffectManager(string effectKey = "Effect/ClickRipple")
        {
            _effectKey = effectKey;

            var go = new GameObject("[EffectCanvas]");
            Object.DontDestroyOnLoad(go);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var scaler = go.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            _canvasTf = go.transform;
        }

        public void OnUpdate()
        {
            if (Input.GetMouseButtonDown(0))
            {
                ResManager.InstantiateFromPoolAsync(_effectKey, go =>
                {
                    if (go == null) return;
                    go.GetComponent<UI_ClickEffect>()?.Play(Input.mousePosition, _canvasTf);
                });
            }
        }
    }
}
