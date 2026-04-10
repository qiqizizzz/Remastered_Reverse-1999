/*
* ┌──────────────────────────────────┐
* │  描    述: 角色接口类                      
* │  类    名: ICharacterState.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Module.Character.Components
{
    public abstract class BaseCharacterState
    {
        protected BaseCharacter _character;

        public BaseCharacterState(BaseCharacter character)
        {
            _character = character;
        }
        
        //进入状态时
        public virtual void OnEnter(){}
        
        //更新状态时
        public virtual void OnUpdate(){}
        
        //离开状态时
        public virtual void OnExit(){}
        
        //spine动画播放完成时
        public virtual void OnAnimationComplete(string animName){}
        
        //spine动画触发事件时
        public virtual void OnAnimationEvent(string eventName){}
    }
}