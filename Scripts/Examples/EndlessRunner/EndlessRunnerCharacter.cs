using System.Collections.Generic;
using UnityEngine;
namespace ChaseMacMillan.CurveDesigner.Examples
{
    public class EndlessRunnerCharacter : MonoBehaviour
    {
        public ObjectOnCurve objectOnCurve;
        public Transform localRoot;
        public Animator animator;
        public Vector2 crosswiseBounds = new Vector2(0,1);
        public float gravity = -9.8f;
        public float jumpSpeed = 10;
        public float runSpeed = 1;
        public float crosswiseMaxSpeed = 1;
        public float crosswiseAcceleration = 1;
        public float maxTurnLean = 5;
        public List<AudioClip> footstepClips;
        public AudioClip JumpStartClip;
        public AudioClip HitGroundClip;
        public GameObject dirtPuff;
        public Transform dirtPuffRoot;
        [System.NonSerialized]
        public int coinCount=0;
        [System.NonSerialized]
        public int health=4;
        public float landHeight = 0;
        private float crosswiseVelocity=0;
        private float verticalVelocity=0;
        private float height=0;
        private bool isGrounded = true;
        private Vector3 initialLocalPosition;
        private float jumpApex;
        private bool hasJumpApex = false;
        private void Start()
        {
            initialLocalPosition = localRoot.localPosition;
        }
        void Update()
        {
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            {
                crosswiseVelocity -= crosswiseAcceleration * Time.deltaTime;
            }
            else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            {
                crosswiseVelocity += crosswiseAcceleration * Time.deltaTime;
            }
            else
            {
                crosswiseVelocity = Mathf.MoveTowards(crosswiseVelocity, 0, crosswiseAcceleration * Time.deltaTime);
            }
            crosswiseVelocity = Mathf.Clamp(crosswiseVelocity, -crosswiseMaxSpeed, crosswiseMaxSpeed);
            if (objectOnCurve.crosswisePosition < crosswiseBounds.x)
            {
                objectOnCurve.crosswisePosition = crosswiseBounds.x;
                crosswiseVelocity = 0;
            }
            else if (objectOnCurve.crosswisePosition > crosswiseBounds.y)
            {
                objectOnCurve.crosswisePosition = crosswiseBounds.y;
                crosswiseVelocity = 0;
            }
            objectOnCurve.crosswisePosition += crosswiseVelocity * Time.deltaTime;
            if (isGrounded)
            {
                if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W))
                {
                    var currState = animator.GetCurrentAnimatorStateInfo(0);
                    var name = currState.shortNameHash;
                    if (name == Animator.StringToHash("Run-right"))
                    {
                        if (currState.normalizedTime > .15 && currState.normalizedTime < .65f)
                            animator.SetBool("wantsJumpLeft", true);
                        else
                            animator.SetBool("wantsJumpRight", true);
                    }
                    else if (name == Animator.StringToHash("Land-right"))
                            animator.SetBool("wantsJumpLeft", true);
                    else if (name == Animator.StringToHash("Land-left"))
                            animator.SetBool("wantsJumpRight", true);
                    else if (name == Animator.StringToHash("Run-left"))
                    {
                        if (currState.normalizedTime > .15 && currState.normalizedTime < .65f)
                            animator.SetBool("wantsJumpRight", true);
                        else
                            animator.SetBool("wantsJumpLeft", true);
                    }
                    PlaySound.Play(JumpStartClip, transform.position,Random.Range(.4f,.5f),Random.Range(.95f,1f));
                    verticalVelocity = jumpSpeed;
                    isGrounded = false;
                    hasJumpApex = false;
                }
                var point = objectOnCurve.curve.GetPointAtDistanceAlongCurve(objectOnCurve.lengthwisePosition);
                transform.rotation = Quaternion.AngleAxis(maxTurnLean * (crosswiseVelocity / crosswiseMaxSpeed), transform.up)*transform.rotation;
            }
            else
            {
                float blend;
                if (verticalVelocity < 0)
                {
                    if (!hasJumpApex)
                    {
                        hasJumpApex = true;
                        jumpApex = height;
                    }
                    blend = Mathf.Clamp01(1 - ((height - landHeight) / (jumpApex - landHeight)));
                }
                else
                {
                    blend = 0;
                }
                animator.SetFloat("Blend", blend);
                verticalVelocity += gravity * Time.deltaTime;
                height += verticalVelocity * Time.deltaTime;
                if (height <= 0)
                {
                    height = 0;
                    isGrounded = true;
                    PlaySound.Play(HitGroundClip, transform.position,Random.Range(.4f,.5f),Random.Range(.9f,1.1f));
                    var puff = Instantiate(dirtPuff);
                    puff.transform.position = dirtPuffRoot.transform.position;
                    puff.transform.rotation = dirtPuffRoot.transform.rotation;
                }
            }
            objectOnCurve.lengthwisePosition += runSpeed*Time.deltaTime;
            localRoot.localPosition = initialLocalPosition + new Vector3(0, height, 0);
            animator.SetBool("wantsLand", height<landHeight && verticalVelocity<0);
        }
        public void PlayFootstep()
        {
            if (isGrounded)
            {
                PlaySound.Play(footstepClips[Random.Range(0, footstepClips.Count)], transform.position,Random.Range(.2f,.5f),Random.Range(.85f,1.1f));
            }
        }
    }
}
