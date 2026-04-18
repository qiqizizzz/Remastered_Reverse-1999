/*
* ┌──────────────────────────────────┐
* │  描    述:                       
* │  类    名: DieState.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Module.Character.Components
{
    public class DieState : BaseCharacterState
    {
        public DieState(BaseCharacter character) : base(character)
        {
        }

        public override void OnEnter()
        {
            _character.PlayAnim(_character.AnimConfig.DieAnim);
        }
    }
}