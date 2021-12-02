using System.Collections.Generic;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public delegate Vector3 PointCreator(PointOnCurve point, int pointNum, int totalPointCount, float size, float rotation, float offset, float arc,ExtrudeSampler extrudeSampler, BezierCurve curve,bool useCachedDistance,out Vector3 normal);
    public static class MeshGeneratorPointCreators
    {
        public static Vector3 ExtrudePointCreator(PointOnCurve point, int currentIndex, int totalPointCount, float size, float rotation, float offset, float arc,ExtrudeSampler extrudeSampler, BezierCurve curve,bool useCachedDistance,out Vector3 normal)
        {
            totalPointCount -= 1;
            float progress = currentIndex / (float)totalPointCount;
            var relativePos = extrudeSampler.SampleAt(point.distanceFromStartOfCurve, progress, curve, out Vector3 reference, out Vector3 tangent, useCachedDistance);//*size;
            var rotationMat = Quaternion.AngleAxis(rotation, point.tangent);
            normal = reference;
            //Lets say z is forward
            var cross = Vector3.Cross(point.tangent, point.reference).normalized;
            Vector3 TransformVector3(Vector3 vect)
            {
                return (Quaternion.LookRotation(point.tangent, point.reference) * vect);
            }
            var absolutePos = point.position + rotationMat * TransformVector3(relativePos);
            Vector3 thicknessDirection = Vector3.Cross(reference, tangent);
            if (Vector3.Dot(TransformVector3(reference), point.tangent) < 0)
                thicknessDirection *= -1;
            return absolutePos + (rotationMat * TransformVector3(thicknessDirection)).normalized * offset;
        }
        public static Vector3 RectanglePointCreator(PointOnCurve point, int currentIndex, int totalPointCount, float size, float rotation, float offset, float arc,ExtrudeSampler extrudeSampler, BezierCurve curve,bool useCachedDistance,out Vector3 normal)
        {
            var center = point.position;
            var up = Quaternion.AngleAxis(rotation, point.tangent) * point.reference;
            var right = Vector3.Cross(up, point.tangent).normalized;
            var scaledUp = up * offset;
            var scaledRight = right * size;
            Vector3 lineStart = center + scaledUp + scaledRight;
            Vector3 lineEnd = center + scaledUp - scaledRight;
            normal = up;
            return Vector3.Lerp(lineStart, lineEnd, currentIndex / (float)(totalPointCount - 1));
        }
        public static Vector3 TubePointCreator(PointOnCurve point, int currentIndex, int totalPointCount, float size, float rotation, float offset, float arc,ExtrudeSampler extrudeSampler, BezierCurve curve,bool useCachedDistance,out Vector3 normal)
        {
            float theta = (arc * currentIndex / (totalPointCount - 1)) + (360.0f - arc) / 2 + rotation;
            var pos = point.GetRingPoint(theta, (size + offset), out normal);
            normal *= Mathf.Sign(offset);
            return pos;
        }
        public static Vector3 TubeFlatPlateCreator(PointOnCurve point, int currentIndex, int totalPointCount, float size, float rotation, float offset, float arc,ExtrudeSampler extrudeSampler, BezierCurve curve,bool useCachedDistance,out Vector3 normal)
        {
            Vector3 lineStart = TubePointCreator(point, 0, totalPointCount, size, rotation, offset, arc,extrudeSampler,curve,useCachedDistance,out _);
            Vector3 lineEnd = TubePointCreator(point, totalPointCount - 1, totalPointCount, size, rotation, offset, arc,extrudeSampler,curve,useCachedDistance,out _);
            float lerp = currentIndex / (float)(totalPointCount - 1);
            normal = Quaternion.AngleAxis(rotation, point.tangent) * point.reference;
            return Vector3.Lerp(lineStart, lineEnd, lerp);
        }
        private static List<(float, Vector3)> GetPositionOnSurface_List = new List<(float, Vector3)>();//to avoid reallocation
        public static Vector3 GetPointOnSurface(Curve3D curve, float distance, float crossAxisDistance, bool front, out float crossAxisWidth)
        {
            var pointOnCurve = curve.positionCurve.GetPointAtDistance(distance);
            GetPointCreatorOffsetAndPointCountByType(curve.type, curve.ringPointCount, curve.flatPointCount, front, out PointCreator pointCreator, out float offset, out int pointCount);
            float size = curve.GetSizeAtDistanceAlongCurve(distance);
            float rotation = curve.GetRotationAtDistanceAlongCurve(distance);
            float arc = curve.GetArcAtDistanceAlongCurve(distance);
            Vector3 previousPoint = Vector3.zero;
            float totalDistance = 0;//distance in local space
            GetPositionOnSurface_List.Clear();
            for (int i = 0; i < pointCount; i++)
            {
                Vector3 point = pointCreator(pointOnCurve, i, pointCount,size,rotation,offset,arc,curve.extrudeSampler,curve.positionCurve,false,out _);//normal
                if (i > 0)
                {
                    totalDistance += Vector3.Distance(previousPoint,point);
                }
                GetPositionOnSurface_List.Add((totalDistance,point));
                previousPoint = point;
            }
            crossAxisWidth = totalDistance;
            if (crossAxisDistance <= 0.0f)
                return curve.transform.TransformPoint(GetPositionOnSurface_List[0].Item2);
            float crossAxis = totalDistance * crossAxisDistance;
            for (int i = 0; i < pointCount - 1; i++)
            {
                if (GetPositionOnSurface_List[i+1].Item1 > crossAxis)
                {
                    float next = GetPositionOnSurface_List[i + 1].Item1;
                    float curr = GetPositionOnSurface_List[i].Item1;
                    float lerp = (crossAxis-curr)/(next-curr);
                    return curve.transform.TransformPoint(Vector3.Lerp(GetPositionOnSurface_List[i].Item2,GetPositionOnSurface_List[i+1].Item2,lerp));
                }
            }
            return curve.transform.TransformPoint(GetPositionOnSurface_List[GetPositionOnSurface_List.Count - 1].Item2);//last point
        }
        private static void GetPointCreatorOffsetAndPointCountByType(MeshGenerationMode curveType,int ringPointCount, int flatPointCount,bool front,out PointCreator pointCreator, out float offset, out int pointCount)
        {
            //This function is hardcoded to match the code in meshgenerator because everything is cleaner this way
            switch (curveType)
            {
                case MeshGenerationMode.Cylinder:
                    if (front)
                    {
                        pointCreator = TubePointCreator;
                        pointCount = ringPointCount;
                        offset = 0;
                    }
                    else
                    {
                        pointCreator = TubeFlatPlateCreator;
                        pointCount = flatPointCount;
                        offset = 0;
                    }
                    break;
                case MeshGenerationMode.HollowTube:
                    pointCreator = TubePointCreator;
                    pointCount = ringPointCount;
                    if (front)
                        offset = 0;
                    else
                        offset = -1;
                    break;
                case MeshGenerationMode.Flat:
                    pointCreator = RectanglePointCreator;
                    pointCount = flatPointCount;
                    if (front)
                        offset = .5f;
                    else
                        offset = -.5f;
                    break;
                case MeshGenerationMode.Extrude:
                    pointCreator = ExtrudePointCreator;
                    pointCount = ringPointCount;
                    if (front)
                        offset = .5f;
                    else
                        offset = -.5f;
                    break;
                default:
                    throw new System.ArgumentException("Not valid for this curve type");
            }
        }
    }
}
