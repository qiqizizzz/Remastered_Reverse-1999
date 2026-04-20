/*
* ┌──────────────────────────────────┐
* │  描    述: 角色基类(包含玩家和怪物) 
* │  类    名: BaseCharacter.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Data.card;
using DG.Tweening;
using Module.Character.Components;
using Spine;
using Spine.Unity;
using UnityEngine;
using Event = Spine.Event;

namespace Module.Character
{
    /// <summary>
    /// 角色状态
    /// </summary>
    public enum CharacterStateType
    {
        Idle, //待机
        Attack, //攻击
        Hurt, //受伤
        Die //死亡
    }

    /// <summary>
    /// 动画配置类
    /// </summary>
    [Serializable]
    public class AnimationConfig
    {
        [SpineAnimation] public string IdleAnim;
        [SpineAnimation] public string AttackAnim;
        [SpineAnimation] public string HurtAnim;
        [SpineAnimation] public string DieAnim;
    }
    
    public class BaseCharacter : MonoBehaviour
    {
        private string _instanceId;
        protected CharacterData _characterData;
        [SerializeField]protected SkeletonAnimation _skeAnim;

        private CharacterHUD _HUD;
        
        [Header("属性相关")]
        public float CurrentHp;
        public float MaxHp;
        
        [Header("状态相关")]
        private Dictionary<CharacterStateType, BaseCharacterState> _stateMachine;
        private BaseCharacterState _currentState;
        public CharacterStateType CurrentStateType;

        [Header("动画映射")] 
        public AnimationConfig AnimConfig;

        public CharacterData CharacterData => _characterData;
        public string InstanceID => _instanceId;
        public CharacterHUD HUD => _HUD;
        
        #region 生命周期、初始化
        protected virtual void Awake()
        {
            _instanceId = Guid.NewGuid().ToString();
            _skeAnim = GetComponentInChildren<SkeletonAnimation>();
            _HUD = GetComponentInChildren<CharacterHUD>();
            
            InitStateMachine();
        }

        protected virtual void Start()
        {
            _skeAnim.AnimationState.Complete += onAnimComplete;
            _skeAnim.AnimationState.Event += onAnimEvent;
        }

        protected virtual void Update()
        {
            _currentState?.OnUpdate();
        }

        protected virtual void OnDestroy()
        {
            if (_skeAnim != null)
            {
                _skeAnim.AnimationState.Complete -= onAnimComplete;
                _skeAnim.AnimationState.Event -= onAnimEvent;
                
                Destroy(gameObject);
            }
        }

        public void Init(CharacterData data)
        {
            _characterData = data;
            MaxHp = data.Property.Hp;
            CurrentHp = MaxHp;
            gameObject.name = _characterData.Name;
            
            _skeAnim.skeleton.A = 1f;
            _HUD?.SetAlpha(1f);
            _HUD?.UpdateHp(CurrentHp, MaxHp);
            _HUD?.SetSelected(false);
            ChangeState(CharacterStateType.Idle);
        }
        
        private void InitStateMachine()
        {
            _stateMachine = new Dictionary<CharacterStateType, BaseCharacterState>
            {
                { CharacterStateType.Idle, new IdleState(this) },
                { CharacterStateType.Attack, new AttackState(this) },
                { CharacterStateType.Hurt, new HurtState(this) },
                { CharacterStateType.Die, new DieState(this) }
            };
        }
        #endregion
        
        public void ChangeState(CharacterStateType newStateType)
        {
            if(CurrentStateType == CharacterStateType.Die) return;
            
            _currentState?.OnExit();
            
            //切换状态
            CurrentStateType = newStateType;
            _currentState = _stateMachine[newStateType];
            
            _currentState?.OnEnter();
        }

        public void PlayAnim(string animName, bool loop = false, int trackIndex = 0)
        {
            if (string.IsNullOrEmpty(animName))
            {
                Debug.LogWarning($"{gameObject.name} 尝试播放一个空的动画");
                return;
            }
            
            _skeAnim.AnimationState.SetAnimation(trackIndex, animName, loop);
        }

        public void TakeDamage(int damage, bool isCrit = false)
        {
            if(CurrentStateType == CharacterStateType.Die) return;

            CurrentHp -= damage;
            CurrentHp = Mathf.Max(0, CurrentHp);

            _HUD?.ShowDamage(damage, isCrit);
            _HUD?.UpdateHp(CurrentHp, MaxHp);
            
            if (CurrentHp <= 0)
                ChangeState(CharacterStateType.Die);
            else
                ChangeState(CharacterStateType.Hurt);
        }

        public float GetAnimDuration(string animName)
        {
            if (_skeAnim == null || string.IsNullOrEmpty(animName)) return 0f;

            var anim = _skeAnim.Skeleton.Data.FindAnimation(animName);
            return anim?.Duration ?? 0f;
        }
        
        public void HandleDie()
        {
            Sequence seq = DOTween.Sequence();
            
            if (_skeAnim != null && _skeAnim.skeleton != null)
                seq.Join(DOTween.To(() => _skeAnim.skeleton.A, x => _skeAnim.skeleton.A = x, 0f, 0.5f));
                
            if (_HUD != null)
                seq.Join(_HUD.DOFade(0f, 0.5f));

            seq.OnComplete(() => gameObject.SetActive(false));
        }

        #region spine事件
        private void onAnimComplete(TrackEntry trackEntry)
        {
            _currentState?.OnAnimationComplete(trackEntry.Animation.Name);
        }

        private void onAnimEvent(TrackEntry trackEntry, Event e)
        {
            _currentState?.OnAnimationEvent(e.Data.Name);
        }
        #endregion
    }
}