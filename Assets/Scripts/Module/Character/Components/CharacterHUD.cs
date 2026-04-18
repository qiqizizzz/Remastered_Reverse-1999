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
        [Header("血条UI")] 
        [SerializeField] private Slider HpSlider;
        
        [Header("飘字配置")]
        [SerializeField] private Transform FloatPoint;

        private void Awake()
        {
            HpSlider = GetComponentInChildren<Slider>();
            FloatPoint = GetComponentInChildren<Transform>().Find("FloatPoint");
        }

        public void UpdateHp(float currentHp, float maxHp)
        {
            HpSlider.value = currentHp / maxHp;
        }

        public void ShowDamage(int damage, bool isCrit)
        {
            if (FloatPoint == null) return;
            
            ResManager.InstantiateFromPoolAsync(AddressDefines.UI_small_Txt_Damage, (go) =>
            {
                //这个也许可以优化？
                TextMeshProUGUI txt = go.GetComponentInChildren<TextMeshProUGUI>();
                
                go.transform.position = FloatPoint.position;
                txt.fontSize = 52;
                
                txt.text = $"- {damage}";
                txt.color = isCrit ? Color.yellow : Color.white;
                if (isCrit) txt.fontSize = Mathf.RoundToInt(txt.fontSize * 1.5f);

                go.transform.DOKill();
                txt.DOKill();
                
                go.transform.DOMoveY(go.transform.position.y + 2f, 1f).SetEase(Ease.OutQuad);
                txt.DOFade(0, 1f).OnComplete(() =>
                {
                    ResManager.ReleaseToPool(AddressDefines.UI_small_Txt_Damage, go);
                });
            }, FloatPoint);
        }
    }
}