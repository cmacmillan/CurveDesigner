using System.Linq;
using UnityEngine;
namespace ChaseMacMillan.CurveDesigner
{
    //This is just a simple interface for interacting with curves. The meat of Curve3D is in Curve3DCore.cs
    [ExecuteAlways]
    public partial class Curve3D : MonoBehaviour, ISerializationCallbackReceiver
    {
        public bool IsClosedLoop => positionCurve.isClosedLoop;
        public float CurveLength => this.positionCurve.GetLength();

        //The position in 3d space at a particular distance along the curve. This function is useful if you need an object to follow along a curve
        public Vector3 GetPositionAtDistanceAlongCurve(float distance)
        {
            var point = positionCurve.GetPointAtDistance(distance, false);
            point.FromLocalToWorld(transform);
            return point.position;
        }

        //The curve's tangent is the vector that extends tangent to the curve at a particular point 
        //See https://en.wikipedia.org/wiki/Tangent
        public Vector3 GetTangentAtDistanceAlongCurve(float distance)
        {
            var point = positionCurve.GetPointAtDistance(distance);
            point.FromLocalToWorld(transform);
            return point.tangent;
        }
        //The 'reference' or 'normal' vector is the vector that is perpendicular to the tangent at a particular point
        //Can be thought of as pointing away from the curve
        //Affected by curve rotation
        public Vector3 GetReferenceAtDistanceAlongCurve(float distance,bool applyRotation=true)
        {
            float angle = GetRotationAtDistanceAlongCurve(distance);
            var point = positionCurve.GetPointAtDistance(distance);
            point.FromLocalToWorld(transform);
            if (applyRotation)
                return Quaternion.AngleAxis(angle, point.tangent) * point.reference;
            else
                return point.reference;
        }

        //This returns a PointOnCurve from a particular distance along the curve. This contains the position, tangent and reference at a point along the curve
        public PointOnCurve GetPointAtDistanceAlongCurve(float distance,bool worldSpace=true)
        {
            var point = positionCurve.GetPointAtDistance(distance);
            //reference not getting rotated here! that's a problem. Gotta make sure changing this won't break internal use-cases tho
            if (worldSpace)
                point.FromLocalToWorld(transform);
            return point;
        }

        public Vector3 GetPointOnSurface(float lengthwise, float crosswise, out Vector3 normal, out Vector3 tangent, bool front = true)
        {
            return MeshGeneratorPointCreators.GetPointOnSurface(this, lengthwise, crosswise, front, out normal, out tangent, out _);
        }

        //The functions below simply return the values of different samples at a distance
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

        public Vector3 GetClosestPositionOnCurve(Vector3 worldPosition)
        {
            return GetClosestPointOnCurve(worldPosition).position;
        }

        //Appends a copy of another curve3d to this curve3d. Position/rotation data of otherCurve is discarded
        public void AppendCurve(Curve3D otherCurve)
        {
            AppendCurve_Internal(otherCurve);
        }
        [ContextMenu("MakeUnique")]
        public void MakeUnique()
        {
            objectsOnThisCurve = objectsOnThisCurve.Distinct().ToList();
        }

        public void DeletePointsBefore(int index)
        {
            throw new System.NotImplementedException();
        }

        public void Awake()
        {
            if (isInitialized)
            {
                Recalculate();
                wantsUpdate = true;
            }
        }
        public void Start()
        {
            Initialize();
            UpdateMesh(false);
        }
        public void Update()
        {
            UpdateMesh(false);
        }
        public void LateUpdate()
        {
            UpdateMesh(false);
        }

#if UNITY_EDITOR
        [ContextMenu("ExportToObj")]
        public void ExportToObj()
        {
            ObjMeshExporter.DoExport(gameObject, false);
        }
#endif
    }
}
