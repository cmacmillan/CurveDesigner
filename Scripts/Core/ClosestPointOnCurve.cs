using System;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    //See https://mathoverflow.net/questions/8983/closest-point-on-bezier-spline for more info
    //A cubic bezier can be evaluated with:
    //Position(t) = (1-t)^3*Point0+3*(1-t)^2*t*Point1+3*(1-t)*t^2*Point2+t^3*Point3
    //This function can be split into 3 components Pos_X(t) Pos_Y(t) Pos_Z(t)
    //The distance of these functions to an arbitrary point origin can be written:
    //SqrDist(t) = (Pos_X(t)-origin.x)^2+(Pos_Y(t)-origin.y)^2+(Pos_Z(t)-origin.z)^2
    //The value of t that minimizes SqrDist(t) will also be a point where the first derivative = 0
    //We can use newtons method with several uniformly spaced starting points to find these points
    //The equations in GetSqrDistCoefs to produce the coefficients are just the result of combining the above equations together, which I did via mathematica
    public static class ClosestPointOnCurve
    {
        public static void GetClosestPoint(BezierCurve curve, Vector3 point, out int bestSegmentIndex, out float bestTime)
        {
            bestSegmentIndex = -1;
            bestTime = 0;
            float minSqrDist = float.MaxValue;
            for (int segmentIndex = 0; segmentIndex < curve.NumSegments; segmentIndex++)
            {
                Vector3 p0 = curve.PointGroups[segmentIndex].GetPositionLocal(PointGroupIndex.Position);
                Vector3 p1 = curve.PointGroups[segmentIndex].GetPositionLocal(PointGroupIndex.RightTangent);
                Vector3 p2 = curve.PointGroups[segmentIndex+1].GetPositionLocal(PointGroupIndex.LeftTangent);
                Vector3 p3 = curve.PointGroups[segmentIndex+1].GetPositionLocal(PointGroupIndex.Position);
                var coefs = GetSqrDistCoefs(p0,p1,p2,p3,point);
                for (int i = 0; i <= pointCount; i++)
                {
                    double t = i / (float)pointCount;
                    float result;
                    if (t==0 || t == 1)
                    {
                        result = (float)t;
                    }else
                    {
                        result = (float)NewtonsMethod(t, coefs);
                        result = Mathf.Clamp01(result);
                    }
                    Vector3 pos = curve.GetSegmentPositionAtTime(segmentIndex,result);
                    float dist = (pos - point).sqrMagnitude;
                    if (dist < minSqrDist)
                    {
                        minSqrDist = dist;
                        bestSegmentIndex = segmentIndex;
                        bestTime = result;
                    }
                }
            }
        } 
        private const int pointCount = 6;
        private const int newtonsMethodIterations = 15;
        private static double NewtonsMethod(double initial, SqrDistCoefs coefs)
        {
            double t = initial;
            for (int i = 0; i < newtonsMethodIterations; i++)
            {
                double t2 = t * t;
                double t3 = t2 * t;
                double t4 = t3 * t;
                double t5 = t4 * t;
                double t6 = t5 * t;
                double firstDer = coefs.aDer*t5+coefs.bDer*t4+coefs.cDer*t3+coefs.dDer*t2+coefs.eDer*t+coefs.fDer;
                double secondDer = coefs.aDer2*t4+coefs.bDer2*t3+coefs.cDer2*t2+coefs.dDer2*t+coefs.eDer2;
                t = t - (firstDer / secondDer);
            }
            return t;
        }

        private static SqrDistCoefs GetSqrDistCoefs(Vector3 p0,Vector3 p1,Vector3 p2,Vector3 p3,Vector3 o)
        {
            SqrDistCoefs coefs=new SqrDistCoefs();
            coefs.g = o.x * o.x + o.y * o.y + o.z * o.z - 2 * o.x * p0.x + p0.x * p0.x - 2 * o.y * p0.y + p0.y * p0.y - 2 * o.z * p0.z + p0.z * p0.z;
            coefs.f = 6 * o.x * p0.x - 6 * p0.x * p0.x + 6 * o.y * p0.y - 6 * p0.y * p0.y + 6 * o.z * p0.z - 6 * p0.z * p0.z - 6 * o.x * p1.x + 6 * p0.x * p1.x - 6 * o.y * p1.y + 6 * p0.y * p1.y - 6 * o.z * p1.z + 6 * p0.z * p1.z;
            coefs.e = -6 * o.x * p0.x + 15 * p0.x * p0.x - 6 * o.y * p0.y + 15 * p0.y * p0.y - 6 * o.z * p0.z + 15 * p0.z * p0.z + 12 * o.x * p1.x - 30 * p0.x * p1.x + 9 * p1.x * p1.x + 12 * o.y * p1.y - 30 * p0.y * p1.y + 9 * p1.y * p1.y + 12 * o.z * p1.z - 30 * p0.z * p1.z + 9 * p1.z * p1.z - 6 * o.x * p2.x + 6 * p0.x * p2.x - 6 * o.y * p2.y + 6 * p0.y * p2.y - 6 * o.z * p2.z + 6 * p0.z * p2.z;
            coefs.d = 2 * o.x * p0.x - 20 * p0.x * p0.x + 2 * o.y * p0.y - 20 * p0.y * p0.y + 2 * o.z * p0.z - 20 * p0.z * p0.z - 6 * o.x * p1.x + 60 * p0.x * p1.x - 36 * p1.x * p1.x - 6 * o.y * p1.y + 60 * p0.y * p1.y - 36 * p1.y * p1.y - 6 * o.z * p1.z + 60 * p0.z * p1.z - 36 * p1.z * p1.z + 6 * o.x * p2.x - 24 * p0.x * p2.x + 18 * p1.x * p2.x + 6 * o.y * p2.y - 24 * p0.y * p2.y + 18 * p1.y * p2.y + 6 * o.z * p2.z - 24 * p0.z * p2.z + 18 * p1.z * p2.z - 2 * o.x * p3.x + 2 * p0.x * p3.x - 2 * o.y * p3.y + 2 * p0.y * p3.y - 2 * o.z * p3.z + 2 * p0.z * p3.z;
            coefs.c = 15 * p0.x * p0.x + 15 * p0.y * p0.y + 15 * p0.z * p0.z - 60 * p0.x * p1.x + 54 * p1.x * p1.x - 60 * p0.y * p1.y + 54 * p1.y * p1.y - 60 * p0.z * p1.z + 54 * p1.z * p1.z + 36 * p0.x * p2.x - 54 * p1.x * p2.x + 9 * p2.x * p2.x + 36 * p0.y * p2.y - 54 * p1.y * p2.y + 9 * p2.y * p2.y + 36 * p0.z * p2.z - 54 * p1.z * p2.z + 9 * p2.z * p2.z - 6 * p0.x * p3.x + 6 * p1.x * p3.x - 6 * p0.y * p3.y + 6 * p1.y * p3.y - 6 * p0.z * p3.z + 6 * p1.z * p3.z;
            coefs.b = -6 * p0.x * p0.x - 6 * p0.y * p0.y - 6 * p0.z * p0.z + 30 * p0.x * p1.x - 36 * p1.x * p1.x + 30 * p0.y * p1.y - 36 * p1.y * p1.y + 30 * p0.z * p1.z - 36 * p1.z * p1.z - 24 * p0.x * p2.x + 54 * p1.x * p2.x - 18 * p2.x * p2.x - 24 * p0.y * p2.y + 54 * p1.y * p2.y - 18 * p2.y * p2.y - 24 * p0.z * p2.z + 54 * p1.z * p2.z - 18 * p2.z * p2.z + 6 * p0.x * p3.x - 12 * p1.x * p3.x + 6 * p2.x * p3.x + 6 * p0.y * p3.y - 12 * p1.y * p3.y + 6 * p2.y * p3.y + 6 * p0.z * p3.z - 12 * p1.z * p3.z + 6 * p2.z * p3.z;
            coefs.a = p0.x * p0.x + p0.y * p0.y + p0.z * p0.z - 6 * p0.x * p1.x + 9 * p1.x * p1.x - 6 * p0.y * p1.y + 9 * p1.y * p1.y - 6 * p0.z * p1.z + 9 * p1.z * p1.z + 6 * p0.x * p2.x - 18 * p1.x * p2.x + 9 * p2.x * p2.x + 6 * p0.y * p2.y - 18 * p1.y * p2.y + 9 * p2.y * p2.y + 6 * p0.z * p2.z - 18 * p1.z * p2.z + 9 * p2.z * p2.z - 2 * p0.x * p3.x + 6 * p1.x * p3.x - 6 * p2.x * p3.x + p3.x * p3.x - 2 * p0.y * p3.y + 6 * p1.y * p3.y - 6 * p2.y * p3.y + p3.y * p3.y - 2 * p0.z * p3.z + 6 * p1.z * p3.z - 6 * p2.z * p3.z + p3.z * p3.z;

            coefs.aDer = 6 * coefs.a;
            coefs.bDer = 5 * coefs.b;
            coefs.cDer = 4 * coefs.c;
            coefs.dDer = 3 * coefs.d;
            coefs.eDer = 2 * coefs.e;
            coefs.fDer = coefs.f;

            coefs.aDer2 = 5*coefs.aDer;
            coefs.bDer2 = 4*coefs.bDer;
            coefs.cDer2 = 3*coefs.cDer;
            coefs.dDer2 = 2*coefs.dDer;
            coefs.eDer2 = coefs.eDer;
            return coefs;
        }
        private struct SqrDistCoefs
        {
            //The coefs for the distance function
            public double a;
            public double b;
            public double c;
            public double d;
            public double e;
            public double f;
            public double g;

            //The coefs of the first derivative of the distance function
            public double aDer;
            public double bDer;
            public double cDer;
            public double dDer;
            public double eDer;
            public double fDer;

            //The coefs of the second derivative of the distance function
            public double aDer2;
            public double bDer2;
            public double cDer2;
            public double dDer2;
            public double eDer2;
        }
    }
}
