using System.Collections.Generic;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public delegate Vector3 PointCreator(PointOnCurve point, int pointNum, int totalPointCount, float size, float rotation, float offset, float arc,ExtrudeSampler extrudeSampler, BezierCurve curve,bool useCachedDistance,out Vector3 normal);
    public static class MeshGeneratorPointCreators
    {
        public static Vector3 ExtrudePointCreator_Core(PointOnCurve point, float lerp, float size, float rotation, float offset, float arc, ExtrudeSampler extrudeSampler, BezierCurve curve, bool useCachedDistance, out Vector3 normal)
        {
            var relativePos = extrudeSampler.SampleAt(point.distanceFromStartOfCurve, lerp, curve, out Vector3 reference, out Vector3 tangent, useCachedDistance);//*size;
            var rotationMat = Quaternion.AngleAxis(rotation, point.tangent);
            //Lets say z is forward
            var cross = Vector3.Cross(point.tangent, point.reference).normalized;
            Vector3 TransformVector3(Vector3 vect)
            {
                return (Quaternion.LookRotation(point.tangent, point.reference) * vect);
            }
            var absolutePos = point.position + rotationMat * TransformVector3(relativePos);
            Vector3 thicknessDirection = Vector3.Cross(reference, tangent);
            thicknessDirection = TransformVector3(thicknessDirection).normalized;
            normal = Mathf.Sign(offset)*thicknessDirection;
            return absolutePos + (rotationMat * thicknessDirection) * offset;
        }
        public static Vector3 ExtrudePointCreator(PointOnCurve point, int currentIndex, int totalPointCount, float size, float rotation, float offset, float arc,ExtrudeSampler extrudeSampler, BezierCurve curve,bool useCachedDistance,out Vector3 normal)
        {
            totalPointCount -= 1;
            float lerp = currentIndex / (float)totalPointCount;
            return ExtrudePointCreator_Core(point, lerp, size, rotation, offset, arc, extrudeSampler, curve, useCachedDistance, out normal);
        }
        public static void RectanglePointCreator_Core(PointOnCurve point, float size, float rotation, float offset, BezierCurve curve,out Vector3 normal,out Vector3 lineStart, out Vector3 lineEnd)
        {
            var center = point.position;
            var up = Quaternion.AngleAxis(rotation, point.tangent) * point.reference;
            var right = Vector3.Cross(up, point.tangent).normalized;
            var scaledUp = up * offset;
            var scaledRight = right * size;
            lineStart = center + scaledUp + scaledRight;
            lineEnd = center + scaledUp - scaledRight;
            normal = up;
            normal *= Mathf.Sign(offset);
        }
        public static Vector3 RectanglePointCreator(PointOnCurve point, int currentIndex, int totalPointCount, float size, float rotation, float offset, float arc, ExtrudeSampler extrudeSampler, BezierCurve curve, bool useCachedDistance, out Vector3 normal)
        {
            RectanglePointCreator_Core(point, size, rotation, offset, curve, out normal, out Vector3 lineStart, out Vector3 lineEnd);
            return Vector3.Lerp(lineStart, lineEnd, currentIndex / (float)(totalPointCount - 1));
        }
        public static float GetTubeWidth(float size, float offset,float arc)
        {
            float radius = offset + size;
            return 2 * Mathf.PI * radius * arc / 360.0f;
        }
        public static Vector3 TubePointCreator(PointOnCurve point, int currentIndex, int totalPointCount, float size, float rotation, float offset, float arc,ExtrudeSampler extrudeSampler, BezierCurve curve,bool useCachedDistance,out Vector3 normal)
        {
            return point.GetRingPoint(currentIndex / (float)(totalPointCount - 1), size, offset, arc, rotation, out normal);
        }
        public static void TubeFlatPlateCreator_Core(PointOnCurve point, int totalPointCount, float size, float rotation, float offset, float arc,ExtrudeSampler extrudeSampler, BezierCurve curve,bool useCachedDistance,out Vector3 normal, out Vector3 start, out Vector3 end)
        {
            start = TubePointCreator(point, 0, totalPointCount, size, rotation, offset, arc,extrudeSampler,curve,useCachedDistance,out _);
            end = TubePointCreator(point, totalPointCount - 1, totalPointCount, size, rotation, offset, arc,extrudeSampler,curve,useCachedDistance,out _);
            normal = Quaternion.AngleAxis(rotation, point.tangent) * point.reference;
        }
        public static Vector3 TubeFlatPlateCreator(PointOnCurve point, int currentIndex, int totalPointCount, float size, float rotation, float offset, float arc,ExtrudeSampler extrudeSampler, BezierCurve curve,bool useCachedDistance,out Vector3 normal)
        {
            TubeFlatPlateCreator_Core(point, totalPointCount, size, rotation, offset, arc, extrudeSampler, curve, useCachedDistance, out normal, out Vector3 lineStart, out Vector3 lineEnd);
            float lerp = currentIndex / (float)(totalPointCount - 1);
            return Vector3.Lerp(lineStart, lineEnd, lerp);
        }
        public const float frontOffset = .5f;
        public const float backOffset = -.5f;
        //crosswiseDistance is between 0-1, unless crosswiseDistanceIsNormalized is set to false
        public static Vector3 GetPointOnSurface(Curve3D curve, float lengthwiseDistance, float crosswiseDistance, bool front, out Vector3 normal, out Vector3 tangent,out float crossAxisWidth,bool crosswiseDistanceIsNormalized=true)
        {
            var pointOnCurve = curve.positionCurve.GetPointAtDistance(lengthwiseDistance);
            var localTangent = pointOnCurve.tangent;
            float size = curve.GetSizeAtDistanceAlongCurve(lengthwiseDistance);
            float rotation = curve.GetRotationAtDistanceAlongCurve(lengthwiseDistance);
            float arc = curve.GetArcAtDistanceAlongCurve(lengthwiseDistance);
            float thickness = curve.GetThicknessAtDistanceAlongCurve(lengthwiseDistance);
            Vector3 localPosition;
            Vector3 localNormal;
            float offset;
            if (front)
                offset = frontOffset;
            else
                offset = backOffset;
            offset *= thickness;
            switch (curve.type)
            {
                case MeshGenerationMode.Flat:
                    {
                        RectanglePointCreator_Core(pointOnCurve,size,rotation,offset,curve.positionCurve,out localNormal, out Vector3 start, out Vector3 end);
                        crossAxisWidth = Vector3.Distance(start, end);
                        float lerp;
                        if (crosswiseDistanceIsNormalized)
                            lerp = crosswiseDistance;
                        else
                            lerp = crossAxisWidth * crosswiseDistance;
                        localPosition = Vector3.Lerp(start,end,lerp);
                        break;
                    }
                case MeshGenerationMode.HollowTube:
                    {
                        crossAxisWidth = GetTubeWidth(size, offset, arc);
                        float lerp;
                        if (crosswiseDistanceIsNormalized)
                            lerp = crosswiseDistance;
                        else
                            lerp = crossAxisWidth * crosswiseDistance;
                        localPosition = pointOnCurve.GetRingPoint(lerp, size, offset, arc, rotation, out localNormal);
                        break;
                    }
                case MeshGenerationMode.Cylinder:
                    {
                        if (front)
                        {
                            crossAxisWidth = GetTubeWidth(size, 0, arc);
                            float lerp;
                            if (crosswiseDistanceIsNormalized)
                                lerp = crosswiseDistance;
                            else
                                lerp = crossAxisWidth * crosswiseDistance;
                            localPosition = pointOnCurve.GetRingPoint(lerp, size, 0, arc, rotation, out localNormal);
                        }
                        else
                        {
                            TubeFlatPlateCreator_Core(pointOnCurve, 2, size, rotation, 0, arc, curve.extrudeSampler, curve.positionCurve, false, out localNormal, out Vector3 start, out Vector3 end);
                            crossAxisWidth = Vector3.Distance(start, end);
                            float lerp;
                            if (crosswiseDistanceIsNormalized)
                                lerp = crosswiseDistance;
                            else
                                lerp = crossAxisWidth * crosswiseDistance;
                            localPosition = Vector3.Lerp(start, end, lerp);
                        }
                        break;
                    }
                case MeshGenerationMode.Extrude:
                    {
                        if (!crosswiseDistanceIsNormalized)
                            throw new System.NotSupportedException($"crosswiseDistanceIsNormalized must be true for extrude curves");
                        crossAxisWidth = -1;//this value is not calculated for extrude curves
                        localPosition = ExtrudePointCreator_Core(pointOnCurve, crosswiseDistance, size, rotation, offset, arc, curve.extrudeSampler, curve.positionCurve, false, out localNormal);
                        break;
                    }
                default:
                    throw new System.NotSupportedException($"GetPointOnSurface is not valid for curve type '{curve.type}'");
            }
            tangent = curve.transform.TransformDirection(localTangent);
            normal = curve.transform.TransformDirection(localNormal);
            return curve.transform.TransformPoint(localPosition);
        }
    }
}
