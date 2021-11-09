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
        public static void GetClosestPoint(BezierCurve curve, Vector3 point, out int resultSegmentIndex, out float resultTime)
        {
            int bestSegmentIndex = -1;
            float bestTime = 0;
            float minSqrDist = float.MaxValue;
            void CheckIfIsNewBest(int segmentIndex, float time)
            {
                Vector3 pos = curve.GetSegmentPositionAtTime(segmentIndex, time);
                float dist = (pos - point).sqrMagnitude;
                if (dist < minSqrDist)
                {
                    minSqrDist = dist;
                    bestSegmentIndex = segmentIndex;
                    bestTime = time;
                }
            }
            for (int segmentIndex = 0; segmentIndex < curve.NumSegments; segmentIndex++)
            {
                Vector3 p0 = curve.PointGroups[segmentIndex].GetPositionLocal(PointGroupIndex.Position);
                Vector3 p1 = curve.PointGroups[segmentIndex].GetPositionLocal(PointGroupIndex.RightTangent);
                Vector3 p2 = curve.PointGroups[Utils.ModInt(segmentIndex+1,curve.PointGroups.Count)].GetPositionLocal(PointGroupIndex.LeftTangent);
                Vector3 p3 = curve.PointGroups[Utils.ModInt(segmentIndex+1,curve.PointGroups.Count)].GetPositionLocal(PointGroupIndex.Position);
                var coefs = GetSqrDistCoefs(p0,p1,p2,p3,point);
                for (int i = 0; i <= pointCount; i++)
                {
                    double t = i / (float)pointCount;
                    float result = (float)NewtonsMethod(t, coefs);
                    result = Mathf.Clamp01(result);
                    CheckIfIsNewBest(segmentIndex, result);
                }
                CheckIfIsNewBest(segmentIndex, 0);
                CheckIfIsNewBest(segmentIndex, 1);
            }
            resultSegmentIndex = bestSegmentIndex;
            resultTime = bestTime;
        }
        private const int pointCount = 6;
        private const int newtonsMethodIterations = 15;
        private static double NewtonsMethod(double initial, Coefs coefs)
        {
            double t = initial;
            for (int i = 0; i < newtonsMethodIterations; i++)
            {
                double t2 = t * t;
                double t3 = t2 * t;
                double t4 = t3 * t;
                double t5 = t4 * t;
                double firstDer = coefs.aDer*t5+coefs.bDer*t4+coefs.cDer*t3+coefs.dDer*t2+coefs.eDer*t+coefs.fDer;
                double secondDer = coefs.aDer2*t4+coefs.bDer2*t3+coefs.cDer2*t2+coefs.dDer2*t+coefs.eDer2;
                t = t - (firstDer / secondDer);
            }
            return t;
        }

        private static Coefs GetSqrDistCoefs(Vector3 p0,Vector3 p1,Vector3 p2,Vector3 p3,Vector3 o)
        {
            Coefs coefs=new Coefs();
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
            double ox = o.x;
            double oy = o.y;
            double oz = o.z;

            coefs.g = ox * ox + oy * oy + oz * oz - 2 * ox * p0x + p0x * p0x - 2 * oy * p0y + p0y * p0y - 2 * oz * p0z + p0z * p0z;
            coefs.f = 6 * ox * p0x - 6 * p0x * p0x + 6 * oy * p0y - 6 * p0y * p0y + 6 * oz * p0z - 6 * p0z * p0z - 6 * ox * p1x + 6 * p0x * p1x - 6 * oy * p1y + 6 * p0y * p1y - 6 * oz * p1z + 6 * p0z * p1z;
            coefs.e = -6 * ox * p0x + 15 * p0x * p0x - 6 * oy * p0y + 15 * p0y * p0y - 6 * oz * p0z + 15 * p0z * p0z + 12 * ox * p1x - 30 * p0x * p1x + 9 * p1x * p1x + 12 * oy * p1y - 30 * p0y * p1y + 9 * p1y * p1y + 12 * oz * p1z - 30 * p0z * p1z + 9 * p1z * p1z - 6 * ox * p2x + 6 * p0x * p2x - 6 * oy * p2y + 6 * p0y * p2y - 6 * oz * p2z + 6 * p0z * p2z;
            coefs.d = 2 * ox * p0x - 20 * p0x * p0x + 2 * oy * p0y - 20 * p0y * p0y + 2 * oz * p0z - 20 * p0z * p0z - 6 * ox * p1x + 60 * p0x * p1x - 36 * p1x * p1x - 6 * oy * p1y + 60 * p0y * p1y - 36 * p1y * p1y - 6 * oz * p1z + 60 * p0z * p1z - 36 * p1z * p1z + 6 * ox * p2x - 24 * p0x * p2x + 18 * p1x * p2x + 6 * oy * p2y - 24 * p0y * p2y + 18 * p1y * p2y + 6 * oz * p2z - 24 * p0z * p2z + 18 * p1z * p2z - 2 * ox * p3x + 2 * p0x * p3x - 2 * oy * p3y + 2 * p0y * p3y - 2 * oz * p3z + 2 * p0z * p3z;
            coefs.c = 15 * p0x * p0x + 15 * p0y * p0y + 15 * p0z * p0z - 60 * p0x * p1x + 54 * p1x * p1x - 60 * p0y * p1y + 54 * p1y * p1y - 60 * p0z * p1z + 54 * p1z * p1z + 36 * p0x * p2x - 54 * p1x * p2x + 9 * p2x * p2x + 36 * p0y * p2y - 54 * p1y * p2y + 9 * p2y * p2y + 36 * p0z * p2z - 54 * p1z * p2z + 9 * p2z * p2z - 6 * p0x * p3x + 6 * p1x * p3x - 6 * p0y * p3y + 6 * p1y * p3y - 6 * p0z * p3z + 6 * p1z * p3z;
            coefs.b = -6 * p0x * p0x - 6 * p0y * p0y - 6 * p0z * p0z + 30 * p0x * p1x - 36 * p1x * p1x + 30 * p0y * p1y - 36 * p1y * p1y + 30 * p0z * p1z - 36 * p1z * p1z - 24 * p0x * p2x + 54 * p1x * p2x - 18 * p2x * p2x - 24 * p0y * p2y + 54 * p1y * p2y - 18 * p2y * p2y - 24 * p0z * p2z + 54 * p1z * p2z - 18 * p2z * p2z + 6 * p0x * p3x - 12 * p1x * p3x + 6 * p2x * p3x + 6 * p0y * p3y - 12 * p1y * p3y + 6 * p2y * p3y + 6 * p0z * p3z - 12 * p1z * p3z + 6 * p2z * p3z;
            coefs.a = p0x * p0x + p0y * p0y + p0z * p0z - 6 * p0x * p1x + 9 * p1x * p1x - 6 * p0y * p1y + 9 * p1y * p1y - 6 * p0z * p1z + 9 * p1z * p1z + 6 * p0x * p2x - 18 * p1x * p2x + 9 * p2x * p2x + 6 * p0y * p2y - 18 * p1y * p2y + 9 * p2y * p2y + 6 * p0z * p2z - 18 * p1z * p2z + 9 * p2z * p2z - 2 * p0x * p3x + 6 * p1x * p3x - 6 * p2x * p3x + p3x * p3x - 2 * p0y * p3y + 6 * p1y * p3y - 6 * p2y * p3y + p3y * p3y - 2 * p0z * p3z + 6 * p1z * p3z - 6 * p2z * p3z + p3z * p3z;

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
        private struct Coefs
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
