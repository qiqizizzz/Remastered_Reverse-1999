/*
* ┌──────────────────────────────────┐
* │  描    述: 角色HUD                      
* │  类    名: CharacterHUD.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using Common;
using Common.Defines;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Module.Character.Components
{
    public class CharacterHUD : MonoBehaviour
    {
        [Header("UI相关")] 
        private Slider _hpSlider;
        private Image _selectImg;
        
        [Header("飘字配置")]
        private Transform _floatPoint;
        
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _hpSlider = GetComponentInChildren<Slider>();
            _selectImg = transform.Find("Select")?.GetComponent<Image>();
            _floatPoint = GetComponentInChildren<Transform>().Find("FloatPoint");
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        public void UpdateHp(float currentHp, float maxHp)
        {
            _hpSlider.value = currentHp / maxHp;
        }

        public void ShowDamage(int damage, bool isCrit)
        {
            if (_floatPoint == null) return;
            
            ResManager.InstantiateFromPoolAsync(AddressDefines.UI_small_Txt_Damage, (go) =>
            {
                //这个也许可以优化？
                TextMeshProUGUI txt = go.GetComponentInChildren<TextMeshProUGUI>();
                
                go.transform.position = _floatPoint.position;
                txt.fontSize = 52;
                
                txt.text = $"-{damage}";
                txt.color = isCrit ? Color.yellow : Color.white;
                if (isCrit) txt.fontSize = Mathf.RoundToInt(txt.fontSize * 1.5f);

                go.transform.DOKill();
                txt.DOKill();
                
                go.transform.DOMoveY(go.transform.position.y + 2f, 1f).SetEase(Ease.OutQuad);
                txt.DOFade(0, 1f).OnComplete(() =>
                {
                    ResManager.ReleaseToPool(AddressDefines.UI_small_Txt_Damage, go);
                });
            }, _floatPoint);
        }
        
        public void SetAlpha(float alpha) => _canvasGroup.alpha = alpha;
        
        public Tween DOFade(float endValue, float duration) => _canvasGroup.DOFade(endValue, duration);

        public void SetSelected(bool selected)
        {
            if (_selectImg == null)
                return;
            
            _selectImg.gameObject.SetActive(selected);
        }
    }
}