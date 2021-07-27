using UnityEngine;
namespace ChaseMacMillan.CurveDesigner
{
    //This is just a simple interface for interacting with curves. The meat of this class is in Curve3DCore.cs
    public partial class Curve3D : MonoBehaviour, ISerializationCallbackReceiver
    {
        public bool IsClosedLoop => this.isClosedLoop;
        public float CurveLength => this.positionCurve.GetLength();

        public Vector3 GetPositionAtDistanceAlongCurve(float distance)
        {
            return positionCurve.GetPointAtDistance(distance,false).position;
        }

        //The curve's tangent is the vector that extends tangent to the curve at a particular point
        //See https://en.wikipedia.org/wiki/Tangent
        public Vector3 GetTangentAtDistanceAlongCurve(float distance)
        {
            return positionCurve.GetPointAtDistance(distance,true).tangent;
        }
        //The 'reference' or 'normal' vector is the vector that is perpendicular to the tangent at a particular point
        //Can be thought of as pointing away from the curve
        //Affected by curve rotation
        public Vector3 GetReferenceAtDistanceAlongCurve(float distance,bool applyRotation=true)
        {
            float angle = GetRotationAtDistanceAlongCurve(distance);
            var point = positionCurve.GetPointAtDistance(distance, true);
            if (applyRotation)
                return Quaternion.AngleAxis(angle, point.tangent) * point.reference;
            else
                return point.reference;
        }

        public PointOnCurve GetPointAtDistanceAlongCurve(float distance)
        {
            return positionCurve.GetPointAtDistance(distance);
        }
        public float GetRotationAtDistanceAlongCurve(float distance)
        {
            return rotationSampler.GetValueAtDistance(distance, positionCurve);
        }
        public float GetSizeAtDistanceAlongCurve(float distance)
        {
            return sizeSampler.GetValueAtDistance(distance,positionCurve);
        }
        public float GetArcAtDistanceAlongCurve(float distance)
        {
            return arcOfTubeSampler.GetValueAtDistance(distance, positionCurve);
        }
        public float GetThicknessAtDistanceAlongCurve(float distance)
        {
            return thicknessSampler.GetValueAtDistance(distance, positionCurve);
        }
        public Color GetColorAtDistanceAlongCurve(float distance)
        {
            return colorSampler.GetValueAtDistance(distance, positionCurve);
        }

        [ContextMenu("ExportToObj")]
        public void ExportToObj()
        {
            ObjMeshExporter.DoExport(gameObject, false);
        }
    }
}
