/*
* ┌──────────────────────────────────┐
* │  描    述: 角色HUD                      
* │  类    名: CharacterHUD.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
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
        private List<Image> _actionPoints;
        
        [Header("飘字配置")]
        private Transform _floatPoint;
        
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _hpSlider = GetComponentInChildren<Slider>();
            _selectImg = transform.Find("Select")?.GetComponent<Image>();
            _floatPoint = GetComponentInChildren<Transform>().Find("FloatPoint");
            _canvasGroup = GetComponent<CanvasGroup>();
            
            _actionPoints = new List<Image>();
            _actionPoints.AddRange(transform.Find("ActionPoint").GetComponentsInChildren<Image>());
        }

        public void UpdateHp(float currentHp, float maxHp)
        {
            _hpSlider.value = currentHp / maxHp;
        }

        /// <summary>
        /// 更新行动点显示
        /// </summary>
        /// <param name="currentTotalAP">当前已拥有的点数</param>
        /// <param name="previewGain">因卡牌放入队列中而即将增加的点数</param>
        public void UpdateActionPoint(int currentTotalAP, int previewGain = 0)
        {
            Debug.Log($"更新行动点显示: 当前点数={currentTotalAP}, 预览增加={previewGain}");
            
            //思路：
            //1.玩家回合时,若 选择卡牌进入队列, 则为闪烁状态
            //2.玩家输出回合时,行动点常量（取消动画状态）
            int confirmedValue = Mathf.Max(0, currentTotalAP - previewGain);
            int totalValue = currentTotalAP;
            
            for (int i = 0; i < _actionPoints.Count; i++)
            {
                Image pointImg = _actionPoints[i];

                pointImg.DOKill();
                
                if (i < confirmedValue) //常量状态
                {
                    pointImg.color = Color.yellow;
                    pointImg.DOFade(1f, 0f);
                }
                else if (i < totalValue) //闪烁状态
                {
                    pointImg.color = Color.yellow;
                    pointImg.DOFade(0.3f, 0.5f).SetLoops(-1, LoopType.Yoyo);
                }
                else //空点位
                {
                    pointImg.color = Color.gray; 
                    pointImg.DOFade(1f, 0f);
                }
            }
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