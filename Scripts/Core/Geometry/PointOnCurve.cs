using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    [System.Serializable]
    public class PointOnCurve : ISegmentTime
    {
        public int segmentIndex;
        public float time;
        public Vector3 tangent;
        public Vector3 reference;
        public Vector3 position;
        /// <summary>
        /// Distance from start of segment
        /// </summary>
        public float distanceFromStartOfSegment;
        public float distanceFromStartOfCurve;
        //PointOnCurves are stored in local space, so if you want to perform world space calculations with them you'll need to call this first to transform them
        public void FromLocalToWorld(Transform t)
        {
            tangent = t.TransformDirection(tangent);
            reference = t.TransformDirection(reference);
            position = t.TransformPoint(position);
        }
        public PointOnCurve(PointOnCurve pointToClone)
        {
            this.time = pointToClone.time;
            this.distanceFromStartOfSegment = pointToClone.distanceFromStartOfSegment;
            this.position = pointToClone.position;
            this.distanceFromStartOfCurve = pointToClone.distanceFromStartOfCurve;
            this.segmentIndex = pointToClone.segmentIndex;
            this.tangent = pointToClone.tangent;
            this.reference = pointToClone.reference;
        }
        public PointOnCurve(float time, float distanceFromStartOfSegment, Vector3 position, int segmentIndex, Vector3 tangent)
        {
            this.time = time;
            this.distanceFromStartOfSegment = distanceFromStartOfSegment;
            this.position = position;
            this.segmentIndex = segmentIndex;
            this.tangent = tangent;
        }

        public Vector3 GetRingPoint(float angle, float length)
        {
            Vector3 rotatedVect = Quaternion.AngleAxis(angle, tangent) * reference;
            return position + rotatedVect * length;
        }

        public void CalculateReference(PointOnCurve previousPoint, Vector3 previousReference, BezierCurve curve)
        {
            if (previousPoint.position == this.position)
            {
                reference = previousReference.normalized;
                return;
            }
            Vector3 DoubleReflectionRMF(Vector3 x0, Vector3 x1, Vector3 t0, Vector3 t1, Vector3 r0)
            {
                Vector3 v1 = x1 - x0;
                float c1 = Vector3.Dot(v1, v1);
                Vector3 rL = r0 - (2.0f / c1) * Vector3.Dot(v1, r0) * v1;
                Vector3 tL = t0 - (2.0f / c1) * Vector3.Dot(v1, t0) * v1;
                Vector3 v2 = t1 - tL;
                float c2 = Vector3.Dot(v2, v2);
                return rL - (2.0f / c2) * Vector3.Dot(v2, rL) * v2;
            }
            if (curve.dimensionLockMode!= DimensionLockMode.none)
            {
                Vector3 up;
                switch (curve.dimensionLockMode)
                {
                    case DimensionLockMode.x:
                        up = Vector3.right;
                        break;
                    case DimensionLockMode.z:
                        up = Vector3.forward;
                        break;
                    case DimensionLockMode.none:
                    case DimensionLockMode.y:
                    default:
                        up = Vector3.up;
                        break;
                }
                reference = Vector3.ProjectOnPlane(up, tangent).normalized;
                return;
            }
            Vector3 dir;
            switch (curve.normalGenerationMode)
            {
                case CurveNormalGenerationMode.MinimumDistance:
                    reference = DoubleReflectionRMF(previousPoint.position, this.position, previousPoint.tangent.normalized, this.tangent.normalized, previousReference);
                    reference = Vector3.ProjectOnPlane(reference, tangent).normalized;
                    return;
                case CurveNormalGenerationMode.BiasTowardsForward:
                    dir = Vector3.forward;
                    break;
                case CurveNormalGenerationMode.BiasTowardsRight:
                    dir = Vector3.right;
                    break;
                default:
                case CurveNormalGenerationMode.BiasTowardsUp:
                    dir = Vector3.up;
                    break;
            }
            reference = Vector3.ProjectOnPlane(dir, tangent).normalized;
        }

        public int SegmentIndex { get { return segmentIndex; } }

        public float Time { get { return time; } }
    }
}
