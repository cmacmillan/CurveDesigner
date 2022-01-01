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
        private const int initialPointCount = 20;
        public static void GetClosestPoint(BezierCurve curve, Ray ray, out int resultSegmentIndex, out float resultTime,TransformBlob blob=null)
        {
            int bestSegmentIndex = -1;
            float bestTime = 0;
            ray.direction = ray.direction.normalized;
            float bestFitness = float.MinValue;
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
                for (int i = 0; i < initialPointCount; i++)
                {
                    double t =(i+1)/(double)(initialPointCount+1);
                    for (int j = 0; j < newtonsIterations; j++)
                    {
                        deriv(p0, p1, p2, p3, ray.direction, t, out double firstDeriv, out double secondDeriv);
                        t = t - firstDeriv / secondDeriv;
                    }
                    t = (t > 1 ? 1 : (t < 0 ? 0 : t));
                    Vector3 pos = GetPos(p0, p1, p2, p3, (float)t);
                    float fitness = Vector3.Dot(ray.direction,pos.normalized);
                    if (fitness > bestFitness)
                    {
                        bestFitness = fitness;
                        bestSegmentIndex = segmentIndex;
                        bestTime = (float)t;
                    }
                }
            }
            resultSegmentIndex = bestSegmentIndex;
            resultTime = bestTime;
        }
        private static void deriv(Vector3 p0,Vector3 p1,Vector3 p2,Vector3 p3,Vector3 r,double t, out double firstDeriv, out double secondDeriv)
        {
            double rx = r.x;
            double ry = r.y;
            double rz = r.z;
            double t2 = t * t;
            double t3 = t * t * t;
            double iT = 1 - t;
            double iT2 = iT * iT;
            double iT3 = iT * iT * iT;
            double p0x = p0.x;
            double p0y = p0.y;
            double p0z = p0.z;
            double p1x = p1.x;
            double p1y = p1.y;
            double p1z = p1.z;
            double p2x = p2.x;
            double p2y = p2.y;
            double p2z = p2.z;
            double p3x = p3.x;
            double p3y = p3.y;
            double p3z = p3.z;
            firstDeriv = (rz * (3 * p3z * t2 - 3 * p2z * t2 + 6 * p2z * iT * t - 6 * p1z * iT * t + 3 * p1z * iT2 - 3 * p0z * iT2) + ry * (3 * p3y * t2 - 3 * p2y * t2 + 6 * p2y * iT * t - 6 * p1y * iT * t + 3 * p1y * iT2 - 3 * p0y * iT2) + rx * (3 * p3x * t2 - 3 * p2x * t2 + 6 * p2x * iT * t - 6 * p1x * iT * t + 3 * p1x * iT2 - 3 * p0x * iT2)) / sqrt(sqr(p3z * t3 + 3 * p2z * iT * t2 + 3 * p1z * iT2 * t + p0z * iT3) + sqr(p3y * t3 + 3 * p2y * iT * t2 + 3 * p1y * iT2 * t + p0y * iT3) + sqr(p3x * t3 + 3 * p2x * iT * t2 + 3 * p1x * iT2 * t + p0x * iT3)) - ((rz * (p3z * t3 + 3 * p2z * iT * t2 + 3 * p1z * iT2 * t + p0z * iT3) + ry * (p3y * t3 + 3 * p2y * iT * t2 + 3 * p1y * iT2 * t + p0y * iT3) + rx * (p3x * t3 + 3 * p2x * iT * t2 + 3 * p1x * iT2 * t + p0x * iT3)) * (2 * (3 * p3z * t2 - 3 * p2z * t2 + 6 * p2z * iT * t - 6 * p1z * iT * t + 3 * p1z * iT2 - 3 * p0z * iT2) * (p3z * t3 + 3 * p2z * iT * t2 + 3 * p1z * iT2 * t + p0z * iT3) + 2 * (3 * p3y * t2 - 3 * p2y * t2 + 6 * p2y * iT * t - 6 * p1y * iT * t + 3 * p1y * iT2 - 3 * p0y * iT2) * (p3y * t3 + 3 * p2y * iT * t2 + 3 * p1y * iT2 * t + p0y * iT3) + 2 * (3 * p3x * t2 - 3 * p2x * t2 + 6 * p2x * iT * t - 6 * p1x * iT * t + 3 * p1x * iT2 - 3 * p0x * iT2) * (p3x * t3 + 3 * p2x * iT * t2 + 3 * p1x * iT2 * t + p0x * iT3))) / (2 * System.Math.Pow(sqr(p3z * t3 + 3 * p2z * iT * t2 + 3 * p1z * iT2 * t + p0z * iT3) + sqr(p3y * t3 + 3 * p2y * iT * t2 + 3 * p1y * iT2 * t + p0y * iT3) + sqr(p3x * t3 + 3 * p2x * iT * t2 + 3 * p1x * iT2 * t + p0x * iT3), (3.0 / 2.0)));
            secondDeriv = (rz * (6 * p3z * t - 12 * p2z * t + 6 * p1z * t + 6 * p2z * iT - 12 * p1z * iT + 6 * p0z * iT) + ry * (6 * p3y * t - 12 * p2y * t + 6 * p1y * t + 6 * p2y * iT - 12 * p1y * iT + 6 * p0y * iT) + rx * (6 * p3x * t - 12 * p2x * t + 6 * p1x * t + 6 * p2x * iT - 12 * p1x * iT + 6 * p0x * iT)) / sqrt(sqr(p3z * t3 + 3 * p2z * iT * t2 + 3 * p1z * iT2 * t + p0z * iT3) + sqr(p3y * t3 + 3 * p2y * iT * t2 + 3 * p1y * iT2 * t + p0y * iT3) + sqr(p3x * t3 + 3 * p2x * iT * t2 + 3 * p1x * iT2 * t + p0x * iT3)) - ((rz * (p3z * t3 + 3 * p2z * iT * t2 + 3 * p1z * iT2 * t + p0z * iT3) + ry * (p3y * t3 + 3 * p2y * iT * t2 + 3 * p1y * iT2 * t + p0y * iT3) + rx * (p3x * t3 + 3 * p2x * iT * t2 + 3 * p1x * iT2 * t + p0x * iT3)) * (2 * sqr(3 * p3z * t2 - 3 * p2z * t2 + 6 * p2z * iT * t - 6 * p1z * iT * t + 3 * p1z * iT2 - 3 * p0z * iT2) + 2 * sqr(3 * p3y * t2 - 3 * p2y * t2 + 6 * p2y * iT * t - 6 * p1y * iT * t + 3 * p1y * iT2 - 3 * p0y * iT2) + 2 * sqr(3 * p3x * t2 - 3 * p2x * t2 + 6 * p2x * iT * t - 6 * p1x * iT * t + 3 * p1x * iT2 - 3 * p0x * iT2) + 2 * (6 * p3z * t - 12 * p2z * t + 6 * p1z * t + 6 * p2z * iT - 12 * p1z * iT + 6 * p0z * iT) * (p3z * t3 + 3 * p2z * iT * t2 + 3 * p1z * iT2 * t + p0z * iT3) + 2 * (6 * p3y * t - 12 * p2y * t + 6 * p1y * t + 6 * p2y * iT - 12 * p1y * iT + 6 * p0y * iT) * (p3y * t3 + 3 * p2y * iT * t2 + 3 * p1y * iT2 * t + p0y * iT3) + 2 * (6 * p3x * t - 12 * p2x * t + 6 * p1x * t + 6 * p2x * iT - 12 * p1x * iT + 6 * p0x * iT) * (p3x * t3 + 3 * p2x * iT * t2 + 3 * p1x * iT2 * t + p0x * iT3))) / (2 * System.Math.Pow(sqr(p3z * t3 + 3 * p2z * iT * t2 + 3 * p1z * iT2 * t + p0z * iT3) + sqr(p3y * t3 + 3 * p2y * iT * t2 + 3 * p1y * iT2 * t + p0y * iT3) + sqr(p3x * t3 + 3 * p2x * iT * t2 + 3 * p1x * iT2 * t + p0x * iT3), 3.0 / 2.0)) - ((rz * (3 * p3z * t2 - 3 * p2z * t2 + 6 * p2z * iT * t - 6 * p1z * iT * t + 3 * p1z * iT2 - 3 * p0z * iT2) + ry * (3 * p3y * t2 - 3 * p2y * t2 + 6 * p2y * iT * t - 6 * p1y * iT * t + 3 * p1y * iT2 - 3 * p0y * iT2) + rx * (3 * p3x * t2 - 3 * p2x * t2 + 6 * p2x * iT * t - 6 * p1x * iT * t + 3 * p1x * iT2 - 3 * p0x * iT2)) * (2 * (3 * p3z * t2 - 3 * p2z * t2 + 6 * p2z * iT * t - 6 * p1z * iT * t + 3 * p1z * iT2 - 3 * p0z * iT2) * (p3z * t3 + 3 * p2z * iT * t2 + 3 * p1z * iT2 * t + p0z * iT3) + 2 * (3 * p3y * t2 - 3 * p2y * t2 + 6 * p2y * iT * t - 6 * p1y * iT * t + 3 * p1y * iT2 - 3 * p0y * iT2) * (p3y * t3 + 3 * p2y * iT * t2 + 3 * p1y * iT2 * t + p0y * iT3) + 2 * (3 * p3x * t2 - 3 * p2x * t2 + 6 * p2x * iT * t - 6 * p1x * iT * t + 3 * p1x * iT2 - 3 * p0x * iT2) * (p3x * t3 + 3 * p2x * iT * t2 + 3 * p1x * iT2 * t + p0x * iT3))) / System.Math.Pow(sqr(p3z * t3 + 3 * p2z * iT * t2 + 3 * p1z * iT2 * t + p0z * iT3) + sqr(p3y * t3 + 3 * p2y * iT * t2 + 3 * p1y * iT2 * t + p0y * iT3) + sqr(p3x * t3 + 3 * p2x * iT * t2 + 3 * p1x * iT2 * t + p0x * iT3), 3.0 / 2.0) + (3 * (rz * (p3z * t3 + 3 * p2z * iT * t2 + 3 * p1z * iT2 * t + p0z * iT3) + ry * (p3y * t3 + 3 * p2y * iT * t2 + 3 * p1y * iT2 * t + p0y * iT3) + rx * (p3x * t3 + 3 * p2x * iT * t2 + 3 * p1x * iT2 * t + p0x * iT3)) * sqr(2 * (3 * p3z * t2 - 3 * p2z * t2 + 6 * p2z * iT * t - 6 * p1z * iT * t + 3 * p1z * iT2 - 3 * p0z * iT2) * (p3z * t3 + 3 * p2z * iT * t2 + 3 * p1z * iT2 * t + p0z * iT3) + 2 * (3 * p3y * t2 - 3 * p2y * t2 + 6 * p2y * iT * t - 6 * p1y * iT * t + 3 * p1y * iT2 - 3 * p0y * iT2) * (p3y * t3 + 3 * p2y * iT * t2 + 3 * p1y * iT2 * t + p0y * iT3) + 2 * (3 * p3x * t2 - 3 * p2x * t2 + 6 * p2x * iT * t - 6 * p1x * iT * t + 3 * p1x * iT2 - 3 * p0x * iT2) * (p3x * t3 + 3 * p2x * iT * t2 + 3 * p1x * iT2 * t + p0x * iT3))) / (4 * System.Math.Pow(sqr(p3z * t3 + 3 * p2z * iT * t2 + 3 * p1z * iT2 * t + p0z * iT3) + sqr(p3y * t3 + 3 * p2y * iT * t2 + 3 * p1y * iT2 * t + p0y * iT3) + sqr(p3x * t3 + 3 * p2x * iT * t2 + 3 * p1x * iT2 * t + p0x * iT3), 5.0 / 2.0));
        }
        private static double sqrt(double d) { return System.Math.Sqrt(d); }
        private static double sqr(double d) { return d*d; }
        //Position(t) = (1-t)^3*Point0+3*(1-t)^2*t*Point1+3*(1-t)*t^2*Point2+t^3*Point3
        private static Vector3 GetPos(Vector3 p0,Vector3 p1,Vector3 p2,Vector3 p3,float t)
        {
            float iT = 1 - t;
            return iT * iT * iT * p0 + 3 * iT * iT * t * p1 + 3 * iT * t * t * p2 + t * t * t * p3;
        }
    }
}
