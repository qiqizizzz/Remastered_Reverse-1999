/*
* ┌──────────────────────────────────┐
* │  描    述:                       
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

        public override void OnExit()
        {
            _character.PlayAnim(_character.AnimConfig.IdleAnim, true);//感觉这里有问题
        }
    }
}