using UnityEngine;
namespace ChaseMacMillan.CurveDesigner.Examples
{
    public class EndlessRunnerCamera : MonoBehaviour
    {
        public Curve3D curve;
        public ObjectOnCurve character;
        public float distanceBehindCharacter;
        public float heightAboveCurve;

        void Update()
        {
            var point = curve.GetPointAtDistanceAlongCurve(character.lengthwisePosition - distanceBehindCharacter);
            transform.position = point.position+point.reference*heightAboveCurve;
            var curveCenterPoint = curve.GetPointOnSurface(character.lengthwisePosition, .5f, out _, out _, character.attachedToFront);
            transform.LookAt(curveCenterPoint,point.reference);
        }
    }
}
