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
        [System.NonSerialized]
        public int coinCount=0;
        [System.NonSerialized]
        public int health=4;
        private float crosswiseVelocity=0;
        private float verticalVelocity=0;
        private float height=0;
        private bool isGrounded = true;
        private Vector3 initialLocalPosition;
        private void Start()
        {
            initialLocalPosition = localRoot.localPosition;
        }
        void Update()
        {
            if (isGrounded)
            {
                if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W))
                {
                    verticalVelocity = jumpSpeed;
                    crosswiseVelocity = 0;
                    isGrounded = false;
                }
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
                objectOnCurve.crosswisePosition += crosswiseVelocity * Time.deltaTime;
                if (objectOnCurve.crosswisePosition <= crosswiseBounds.x)
                {
                    objectOnCurve.crosswisePosition = crosswiseBounds.x;
                    crosswiseVelocity = 0;
                }
                else if (objectOnCurve.crosswisePosition >= crosswiseBounds.y)
                {
                    objectOnCurve.crosswisePosition = crosswiseBounds.y;
                    crosswiseVelocity = 0;
                }
                var point = objectOnCurve.curve.GetPointAtDistanceAlongCurve(objectOnCurve.lengthwisePosition);
                transform.rotation = Quaternion.AngleAxis(maxTurnLean * (crosswiseVelocity / crosswiseMaxSpeed), point.tangent)*transform.rotation;
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
                height += verticalVelocity * Time.deltaTime;
                if (height <= 0)
                {
                    height = 0;
                    isGrounded = true;
                }
            }
            objectOnCurve.lengthwisePosition += runSpeed;
            localRoot.localPosition = initialLocalPosition + new Vector3(0, height, 0);
            animator.SetBool("InAir", !isGrounded);
        }
    }
}
