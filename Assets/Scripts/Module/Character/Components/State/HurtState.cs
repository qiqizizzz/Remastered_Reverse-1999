/*
* ┌──────────────────────────────────┐
* │  描    述: 受伤状态                      
* │  类    名: HurtState.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Module.Character.Components
{
    public class HurtState : BaseCharacterState
    {
        public HurtState(BaseCharacter character) : base(character)
        {
        }

        public override void OnEnter()
        {
            _character.PlayAnim(_character.AnimConfig.HurtAnim, false);
        }

        public override void OnAnimationComplete(string animName)
        {
            if (animName == _character.AnimConfig.HurtAnim)
            {
                _character.ChangeState(CharacterStateType.Idle);
            }
        }
    }
}