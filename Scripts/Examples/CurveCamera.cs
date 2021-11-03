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

        //These values control how quickly the position/rotation will follow the object. Higher values will be less smooth, but will follow more closely 
        /*
        public float positionTrackSpeed = 1;
        public float rotationTrackSpeed = 10;
        [Range(0,1)]
        public float gizmoPos = 0;
        public int seg=0;
        private void OnDrawGizmos()
        {
            float t = gizmoPos;
            float it = (1 - t);
            Vector3 p0=curve.positionCurve.PointGroups[seg].GetPositionLocal(PointGroupIndex.Position);
            Vector3 p1=curve.positionCurve.PointGroups[seg].GetPositionLocal(PointGroupIndex.RightTangent);
            Vector3 p2=curve.positionCurve.PointGroups[seg+1].GetPositionLocal(PointGroupIndex.LeftTangent);
            Vector3 p3=curve.positionCurve.PointGroups[seg+1].GetPositionLocal(PointGroupIndex.Position);
            Vector3 pos = it * it * it * p0 + 3 * it * it * t * p1 + 3 * it * t * t * p2 + t * t * t * p3;
            Gizmos.DrawWireSphere(pos,1);
        }
        */
        private void Start()
        {
            var camPoint = GetCameraPoint();
            transform.position = camPoint.position;
            transform.rotation = Quaternion.LookRotation(target.transform.position - transform.position, Vector3.up);
        }
        void Update()
        {
            var camPoint = GetCameraPoint();
            transform.position = camPoint.position;
            transform.rotation = Quaternion.LookRotation(target.transform.position - transform.position, Vector3.up);
            //transform.position = Vector3.Lerp(transform.position,camPoint.position,Time.deltaTime*positionTrackSpeed);
            //transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(target.transform.position - transform.position, Vector3.up),Time.deltaTime*rotationTrackSpeed);
        }
        PointOnCurve GetCameraPoint()
        {
            var point = curve.GetClosestPointOnCurve(target.transform.position);
            Debug.DrawRay(point.position, Vector3.up);
            return curve.GetPointAtDistanceAlongCurve(point.distanceFromStartOfCurve + offset);
        }
    }
}
