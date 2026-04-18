/*
* ┌──────────────────────────────────┐
* │  描    述: 待机状态                      
* │  类    名: IdleState.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Module.Character.Components
{
    public class IdleState : BaseCharacterState
    {
        public IdleState(BaseCharacter character) : base(character)
        {
        }

        public override void OnEnter()
        {
            _character.PlayAnim(_character.AnimConfig.IdleAnim, true);
        }
    }
}