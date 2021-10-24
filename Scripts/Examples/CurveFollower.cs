//here is an example of how you could make an object follow along a curve
//and point in the direction tangent to the curve
using UnityEngine;
namespace ChaseMacMillan.CurveDesigner
{
    public class CurveFollower : MonoBehaviour
    {
        public Curve3D curve;//Assign this in the inspector
        public float distanceAlongCurve = 0;
        public float speed = 1;
        public void Update()
        {
            distanceAlongCurve += Time.deltaTime * speed;
            PointOnCurve point = curve.GetPointAtDistanceAlongCurve(distanceAlongCurve);
            transform.position = point.position;
            transform.rotation = Quaternion.LookRotation(point.tangent, point.reference);
        }
    }
}
