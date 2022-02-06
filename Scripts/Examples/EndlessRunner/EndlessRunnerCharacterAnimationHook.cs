using UnityEngine;

namespace ChaseMacMillan.CurveDesigner.Examples
{
    public class EndlessRunnerCharacterAnimationHook : MonoBehaviour
    {
        public EndlessRunnerCharacter character;
        public void OnLeaveGround()
        {
            character.animator.SetBool("wantsJumpRight", false);
            character.animator.SetBool("wantsJumpLeft", false);
        }
        public void PlayFootstep()
        {
            character.PlayFootstep();
        }
    }
}
