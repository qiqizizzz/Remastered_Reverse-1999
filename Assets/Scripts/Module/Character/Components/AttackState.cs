/*
* ┌──────────────────────────────────┐
* │  描    述: 攻击状态                      
* │  类    名: AttackState.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Module.Character.Components
{
    public class AttackState : BaseCharacterState
    {
        public AttackState(BaseCharacter character) : base(character)
        {
        }

        public override void OnEnter()
        {
            _character.PlayAnim(_character.AnimConfig.AttackAnim, false);
        }

        public override void OnAnimationComplete(string animName)
        {
            if (animName == _character.AnimConfig.AttackAnim)
            {
                _character.ChangeState(CharacterStateType.Idle);
            }
        }
    }
}