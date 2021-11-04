//This script allows a camera to follow behind an object that is near a curve
//Perfect for following behind a ball rolling down a tube
using UnityEngine;
namespace ChaseMacMillan.CurveDesigner
{
    public class CurveCamera : MonoBehaviour
    {
        public Curve3D curve;
        public Transform target;
        public float offset;//how far ahead/behind of the object should the camera be positioned
        private void Start()
        {
            Update();
        }
        void Update()
        {
            var point = curve.GetClosestPointOnCurve(target.transform.position);
            PointOnCurve camPoint = curve.GetPointAtDistanceAlongCurve(point.distanceFromStartOfCurve + offset);
            transform.position = camPoint.position;
            transform.rotation = Quaternion.LookRotation(target.transform.position - transform.position, Vector3.up);
        }
    }
}
