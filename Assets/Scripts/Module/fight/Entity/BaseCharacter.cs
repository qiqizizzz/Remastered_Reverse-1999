/*
* ┌──────────────────────────────────┐
* │  描    述: 角色基类(包含玩家和怪物) 
* │  类    名: BaseCharacter.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Data.card;
using UnityEngine;

namespace Module.fight
{
    /// <summary>
    /// 角色状态
    /// </summary>
    public enum CharacterState
    {
        Idle, //待机
        Attack, //攻击
        Hurt, //受伤
        Die //死亡
    }
    
    public class BaseCharacter : MonoBehaviour
    {
        private CharacterData _characterData;
        private CharacterState _currentState;

        public BaseCharacter()
        {
            
        }
        
        public BaseCharacter(CharacterData characterData)
        {
            _characterData = characterData;
            _currentState = CharacterState.Idle;
        }

        public void ChangeState(CharacterState newState)
        {
            _currentState = newState;
            
            switch (_currentState)
            {
                case CharacterState.Idle:
                    Idle();
                    break;
                case CharacterState.Attack:
                    Attack();
                    break;
                case CharacterState.Hurt:
                    Hurt();
                    break;
                case CharacterState.Die:
                    Die();
                    break;
                default:
                    Debug.Log("未知状态!");
                    break;
            }
        }

        public virtual void Idle()
        {
            //TODO:播放动画
        }

        public virtual void Attack()
        {
            //TODO:播放动画
        }

        public virtual void Hurt()
        {
            //TODO:播放动画
        }

        public virtual void Die()
        {
            ////TODO:播放动画
        }
    }
}