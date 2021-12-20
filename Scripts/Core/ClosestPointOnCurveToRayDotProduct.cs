using System;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    /*
    This class is very similar to ClosestPointOnCurve.cs and ClosestPointOnCurveToLine.cs,
    and the math within was derived in a similar fashion, so scope those to learn more.
    Basically does newton's method to find the closest point on a bezier to a ray, minimizing the dot product
    */
    public static class ClosestPointOnCurveToRayDotProduct
    {
        private const int newtonsIterations = 10;
        private const int initialPointCount = 5;
        public static void GetClosestPoint(BezierCurve curve, Ray ray, out int resultSegmentIndex, out float resultTime,TransformBlob blob=null)
        {
            int bestSegmentIndex = -1;
            float bestTime = 0;
            ray.direction = ray.direction.normalized;
            float bestFitness = float.MaxValue;
            for (int segmentIndex = 0; segmentIndex < curve.NumSegments; segmentIndex++)
            {
                Vector3 p0 = curve.PointGroups[segmentIndex].GetPositionLocal(PointGroupIndex.Position);
                Vector3 p1 = curve.PointGroups[segmentIndex].GetPositionLocal(PointGroupIndex.RightTangent);
                Vector3 p2 = curve.PointGroups[Utils.ModInt(segmentIndex + 1, curve.PointGroups.Count)].GetPositionLocal(PointGroupIndex.LeftTangent);
                Vector3 p3 = curve.PointGroups[Utils.ModInt(segmentIndex + 1, curve.PointGroups.Count)].GetPositionLocal(PointGroupIndex.Position);
                if (blob != null)
                {
                    p0 = blob.TransformPoint(p0);
                    p1 = blob.TransformPoint(p1);
                    p2 = blob.TransformPoint(p2);
                    p3 = blob.TransformPoint(p3);
                }
                p0 -= ray.origin;
                p1 -= ray.origin;
                p2 -= ray.origin;
                p3 -= ray.origin;
                Coefs coefs = GetCoefs(p0,p1,p2,p3,ray.direction);
                for (int i = 0; i < initialPointCount; i++)
                {
                    float t =(i+1)/(float)(initialPointCount+1);
                    float t2;
                    float t3;
                    Vector3 pos;
                    float k;
                    for (int j = 0; j < newtonsIterations; j++)
                    {
                        pos = GetPos(p0,p1,p2,p3,t);
                        k = pos.magnitude;
                        t2 = t * t;
                        float num = (3 * coefs.t3 * t2 + 2 * coefs.t2 * t + coefs.t1)/k;
                        float denom = (6 * coefs.t3 * t + 2 * coefs.t2)/k;
                        t = t - (num / denom);
                    }
                    t = Mathf.Clamp01(t);
                    pos = GetPos(p0, p1, p2, p3, t);
                    k = pos.magnitude;
                    t2 = t * t;
                    t3 = t * t * t;
                    float fitness = (coefs.t3 * t3 + coefs.t2 * t2 + coefs.t1 * t + coefs.c) / k;
                    if (fitness < bestFitness)
                    {
                        bestFitness = fitness;
                        bestSegmentIndex = segmentIndex;
                        bestTime = t;
                    }
                }
            }
            resultSegmentIndex = bestSegmentIndex;
            resultTime = bestTime;
        }
        //Position(t) = (1-t)^3*Point0+3*(1-t)^2*t*Point1+3*(1-t)*t^2*Point2+t^3*Point3
        private static Vector3 GetPos(Vector3 p0,Vector3 p1,Vector3 p2,Vector3 p3,float t)
        {
            float iT = 1 - t;
            return iT * iT * iT * p0 + 3 * iT * iT * t * p1 + 3 * iT * t * t * p2 + t * t * t * p3;
        }
        private static Coefs GetCoefs(Vector3 p0,Vector3 p1,Vector3 p2,Vector3 p3,Vector3 r)
        {
            Coefs retr = new Coefs();
            retr.t3 = (p3.z-3*p2.z+3*p1.z-p0.z)*r.z+(p3.y-3*p2.y+3*p1.y-p0.y)*r.y+(p3.x-3*p2.x+3*p1.x-p0.x)*r.x;
            retr.t2 = (3*p2.z-6*p1.z+3*p0.z)*r.z+(3*p2.y-6*p1.y+3*p0.y)*r.y+(3*p2.x-6*p1.x+3*p0.x)*r.x;
            retr.t1 = (3*p1.z-3*p0.z)*r.z+(3*p1.y-3*p0.y)*r.y+(3*p1.x-3*p0.x)*r.x;
            retr.c = p0.z * r.z + p0.y * r.y + p0.x * r.x;
            return retr;
        }
        private struct Coefs
        {
            public float t3;
            public float t2;
            public float t1;
            public float c;
        }
    }
}
