/*
* ┌──────────────────────────────────┐
* │  描    述: 死亡状态                      
* │  类    名: DieState.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common.Defines;

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
            GameApp.MessageCenter.PostEvent(EventDefines.OnCharacterDie, _character);
        }

        public override void OnAnimationComplete(string animName)
        {
            if (animName == _character.AnimConfig.DieAnim)
            {
                _character.HandleDie();
            }
        }
    }
}