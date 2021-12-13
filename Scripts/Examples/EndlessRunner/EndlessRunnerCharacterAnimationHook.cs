using UnityEngine;

namespace ChaseMacMillan.CurveDesigner.Examples
{
    public class EndlessRunnerCharacterAnimationHook : MonoBehaviour
    {
        public EndlessRunnerCharacter character;
        public void OnLeaveGround()
        {
            character.OnLeaveGround();
        }
        public void PlayFootstep()
        {
            character.PlayFootstep();
        }
    }
}
