using System;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    //This file further builds on the ideas in ClosestPointOnCurve.cs to calculate the closest point on a curve to a given line
    //Except instead of optimizing for a single paramater (distance along the curve) we optimize for an additional parameter (distance along the line)
    //We do this by using multidimensional newtons method, which involves calculating the inverse-hessian matrix and the gradient
    //You can read more about this procedure here https://en.wikipedia.org/wiki/Newton%27s_method_in_optimization#Higher_dimensions
    public static class ClosestPointOnCurveToLine
    {
        public static void GetClosestPointToLine(BezierCurve curve, Vector3 lineStart, Vector3 lineEnd, out int resultSegmentIndex, out float resultTime,TransformBlob blob=null)
        {
            resultSegmentIndex = -1;
            resultTime = 0;
            float minSqrDist = float.MaxValue;
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
                var coefs = GetCoefs(p0, p1, p2, p3, lineStart, lineEnd);
                for (int linePoint = 0; linePoint <= linePoints; linePoint++)
                {
                    for (int curvePoint = 0; curvePoint <= curvePoints; curvePoint++)
                    {
                        Vec2 value = new Vec2() { x = linePoint/(double)linePoints, y = curvePoint/(double)curvePoints };
                        value = NewtonsMethod(value, coefs);
                        float lineValue = (float)value.x;
                        float curveValue = Mathf.Clamp01((float)value.y);
                        Vector3 linePos = lineStart + (lineEnd - lineStart) * lineValue;
                        Vector3 curvePos = curve.GetSegmentPositionAtTime(segmentIndex, curveValue);
                        if (blob != null)
                            curvePos = blob.TransformPoint(curvePos);
                        float dist = (linePos - curvePos).sqrMagnitude;
                        if (dist < minSqrDist)
                        {
                            minSqrDist = dist;
                            resultTime = curveValue;
                            resultSegmentIndex = segmentIndex;
                        }
                    }
                }
            }
        }

        private const int linePoints = 1;
        private const int curvePoints = 5;
        private const int newtonsMethodIterations = 15;
        //private const double dampingAmount = 1;//might be needed to prevent divergence
        private static Vec2 NewtonsMethod(Vec2 initial, Coefs coefs)
        {
            Vec2 current = initial;
            for (int i = 0; i < newtonsMethodIterations; i++)
            {
                double s = current.x;
                double t = current.y;
                double t2 = t * t;
                double t3 = t2 * t;
                double t4 = t3 * t;
                double t5 = t4 * t;
                double st = s * t;
                double stt = s * t2;

                Matrix2x2 hessian = new Matrix2x2();
                double diagonal=coefs.hessianDiagonal_t2*t2+coefs.hessianDiagonal_t*t+coefs.hessianDiagonal_constant;
                hessian.m00 = coefs.hessianUpperLeft_constant;
                hessian.m01 = diagonal;
                hessian.m10 = diagonal;
                hessian.m11 = coefs.hessianLowerRight_t4*t4+coefs.hessianLowerRight_t3*t3+coefs.hessianLowerRight_t2*t2+coefs.hessianLowerRight_t*t+coefs.hessianLowerRight_st*st+coefs.hessianLowerRight_s*s+coefs.hessianLowerRight_constant;
                hessian.Inverse();

                Vec2 gradient = new Vec2();
                gradient.x = coefs.sDiff_t3*t3+coefs.sDiff_t2*t2+coefs.sDiff_t*t+coefs.sDiff_s*s+coefs.sDiff_constant;
                gradient.y = coefs.tDiff_t5*t5+coefs.tDiff_t4*t4+coefs.tDiff_t3*t3+coefs.tDiff_t2*t2+coefs.tDiff_t*t+coefs.tDiff_stt*stt+coefs.tDiff_st*st+coefs.tDiff_s*s+coefs.tDiff_constant;
                current = current - (hessian*gradient);
            }
            return current;
        }
        private static Coefs GetCoefs(Vector3 p0,Vector3 p1,Vector3 p2, Vector3 p3, Vector3 l0, Vector3 l1)
        {
            Coefs coefs = new Coefs();
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
            double l0x = l0.x;
            double l0y = l0.y;
            double l0z = l0.z;
            double l1x = l1.x;
            double l1y = l1.y;
            double l1z = l1.z;

            coefs.hessianUpperLeft_constant = 2 * l1z * l1z - 4 * l0z * l1z + 2 * l1y * l1y - 4 * l0y * l1y + 2 * l1x * l1x - 4 * l0x * l1x + 2 * l0z * l0z + 2 * l0y * l0y + 2 * l0x * l0x;
            coefs.hessianDiagonal_t2 = ((-6 * l1z + 6 * l0z) * p3z + (-6 * l1y + 6 * l0y) * p3y + (-6 * l1x + 6 * l0x) * p3x + (18 * l1z - 18 * l0z) * p2z + (18 * l1y - 18 * l0y) * p2y + (18 * l1x - 18 * l0x) * p2x + (-18 * l1z + 18 * l0z) * p1z + (-18 * l1y + 18 * l0y) * p1y + (-18 * l1x + 18 * l0x) * p1x + (6 * l1z - 6 * l0z) * p0z + (6 * l1y - 6 * l0y) * p0y + (6 * l1x - 6 * l0x) * p0x);
            coefs.hessianDiagonal_t = ((-12 * l1z + 12 * l0z) * p2z + (-12 * l1y + 12 * l0y) * p2y + (-12 * l1x + 12 * l0x) * p2x + (24 * l1z - 24 * l0z) * p1z + (24 * l1y - 24 * l0y) * p1y + (24 * l1x - 24 * l0x) * p1x + (-12 * l1z + 12 * l0z) * p0z + (-12 * l1y + 12 * l0y) * p0y + (-12 * l1x + 12 * l0x) * p0x);
            coefs.hessianDiagonal_constant = (-6 * l1z + 6 * l0z) * p1z + (-6 * l1y + 6 * l0y) * p1y + (-6 * l1x + 6 * l0x) * p1x + (6 * l1z - 6 * l0z) * p0z + (6 * l1y - 6 * l0y) * p0y + (6 * l1x - 6 * l0x) * p0x;
            coefs.hessianLowerRight_t4 = (30 * p3z * p3z + (-180 * p2z + 180 * p1z - 60 * p0z) * p3z + 30 * p3y * p3y + (-180 * p2y + 180 * p1y - 60 * p0y) * p3y + 30 * p3x * p3x + (-180 * p2x + 180 * p1x - 60 * p0x) * p3x + 270 * p2z * p2z + (-540 * p1z + 180 * p0z) * p2z + 270 * p2y * p2y + (-540 * p1y + 180 * p0y) * p2y + 270 * p2x * p2x + (-540 * p1x + 180 * p0x) * p2x + 270 * p1z * p1z - 180 * p0z * p1z + 270 * p1y * p1y - 180 * p0y * p1y + 270 * p1x * p1x - 180 * p0x * p1x + 30 * p0z * p0z + 30 * p0y * p0y + 30 * p0x * p0x);
            coefs.hessianLowerRight_t3 = ((120 * p2z - 240 * p1z + 120 * p0z) * p3z + (120 * p2y - 240 * p1y + 120 * p0y) * p3y + (120 * p2x - 240 * p1x + 120 * p0x) * p3x - 360 * p2z * p2z + (1080 * p1z - 480 * p0z) * p2z - 360 * p2y * p2y + (1080 * p1y - 480 * p0y) * p2y - 360 * p2x * p2x + (1080 * p1x - 480 * p0x) * p2x - 720 * p1z * p1z + 600 * p0z * p1z - 720 * p1y * p1y + 600 * p0y * p1y - 720 * p1x * p1x + 600 * p0x * p1x - 120 * p0z * p0z - 120 * p0y * p0y - 120 * p0x * p0x);
            coefs.hessianLowerRight_t2 = ((72 * p1z - 72 * p0z) * p3z + (72 * p1y - 72 * p0y) * p3y + (72 * p1x - 72 * p0x) * p3x + 108 * p2z * p2z + (-648 * p1z + 432 * p0z) * p2z + 108 * p2y * p2y + (-648 * p1y + 432 * p0y) * p2y + 108 * p2x * p2x + (-648 * p1x + 432 * p0x) * p2x + 648 * p1z * p1z - 720 * p0z * p1z + 648 * p1y * p1y - 720 * p0y * p1y + 648 * p1x * p1x - 720 * p0x * p1x + 180 * p0z * p0z + 180 * p0y * p0y + 180 * p0x * p0x);
            coefs.hessianLowerRight_t = ((12 * p0z - 12 * l0z) * p3z + (12 * p0y - 12 * l0y) * p3y + (12 * p0x - 12 * l0x) * p3x + (108 * p1z - 144 * p0z + 36 * l0z) * p2z + (108 * p1y - 144 * p0y + 36 * l0y) * p2y + (108 * p1x - 144 * p0x + 36 * l0x) * p2x - 216 * p1z * p1z + (360 * p0z - 36 * l0z) * p1z - 216 * p1y * p1y + (360 * p0y - 36 * l0y) * p1y - 216 * p1x * p1x + (360 * p0x - 36 * l0x) * p1x - 120 * p0z * p0z + 12 * l0z * p0z - 120 * p0y * p0y + 12 * l0y * p0y - 120 * p0x * p0x + 12 * l0x * p0x);
            coefs.hessianLowerRight_st = ((-12 * l1z + 12 * l0z) * p3z + (-12 * l1y + 12 * l0y) * p3y + (-12 * l1x + 12 * l0x) * p3x + (36 * l1z - 36 * l0z) * p2z + (36 * l1y - 36 * l0y) * p2y + (36 * l1x - 36 * l0x) * p2x + (-36 * l1z + 36 * l0z) * p1z + (-36 * l1y + 36 * l0y) * p1y + (-36 * l1x + 36 * l0x) * p1x + (12 * l1z - 12 * l0z) * p0z + (12 * l1y - 12 * l0y) * p0y + (12 * l1x - 12 * l0x) * p0x);
            coefs.hessianLowerRight_s = ((-12 * l1z + 12 * l0z) * p2z + (-12 * l1y + 12 * l0y) * p2y + (-12 * l1x + 12 * l0x) * p2x + (24 * l1z - 24 * l0z) * p1z + (24 * l1y - 24 * l0y) * p1y + (24 * l1x - 24 * l0x) * p1x + (-12 * l1z + 12 * l0z) * p0z + (-12 * l1y + 12 * l0y) * p0y + (-12 * l1x + 12 * l0x) * p0x);
            coefs.hessianLowerRight_constant = (12 * p0z - 12 * l0z) * p2z + (12 * p0y - 12 * l0y) * p2y + (12 * p0x - 12 * l0x) * p2x + 18 * p1z * p1z + (-60 * p0z + 24 * l0z) * p1z + 18 * p1y * p1y + (-60 * p0y + 24 * l0y) * p1y + 18 * p1x * p1x + (-60 * p0x + 24 * l0x) * p1x + 30 * p0z * p0z - 12 * l0z * p0z + 30 * p0y * p0y - 12 * l0y * p0y + 30 * p0x * p0x - 12 * l0x * p0x;

            coefs.sDiff_t3 = ((-2 * l1z + 2 * l0z) * p3z + (-2 * l1y + 2 * l0y) * p3y + (-2 * l1x + 2 * l0x) * p3x + (6 * l1z - 6 * l0z) * p2z + (6 * l1y - 6 * l0y) * p2y + (6 * l1x - 6 * l0x) * p2x + (-6 * l1z + 6 * l0z) * p1z + (-6 * l1y + 6 * l0y) * p1y + (-6 * l1x + 6 * l0x) * p1x + (2 * l1z - 2 * l0z) * p0z + (2 * l1y - 2 * l0y) * p0y + (2 * l1x - 2 * l0x) * p0x);
            coefs.sDiff_t2 = ((-6 * l1z + 6 * l0z) * p2z + (-6 * l1y + 6 * l0y) * p2y + (-6 * l1x + 6 * l0x) * p2x + (12 * l1z - 12 * l0z) * p1z + (12 * l1y - 12 * l0y) * p1y + (12 * l1x - 12 * l0x) * p1x + (-6 * l1z + 6 * l0z) * p0z + (-6 * l1y + 6 * l0y) * p0y + (-6 * l1x + 6 * l0x) * p0x);
            coefs.sDiff_t = ((-6 * l1z + 6 * l0z) * p1z + (-6 * l1y + 6 * l0y) * p1y + (-6 * l1x + 6 * l0x) * p1x + (6 * l1z - 6 * l0z) * p0z + (6 * l1y - 6 * l0y) * p0y + (6 * l1x - 6 * l0x) * p0x);
            coefs.sDiff_s = (2 * l1z * l1z - 4 * l0z * l1z + 2 * l1y * l1y - 4 * l0y * l1y + 2 * l1x * l1x - 4 * l0x * l1x + 2 * l0z * l0z + 2 * l0y * l0y + 2 * l0x * l0x);
            coefs.sDiff_constant = (-2 * l1z + 2 * l0z) * p0z + (-2 * l1y + 2 * l0y) * p0y + (-2 * l1x + 2 * l0x) * p0x + 2 * l0z * l1z + 2 * l0y * l1y + 2 * l0x * l1x - 2 * l0z * l0z - 2 * l0y * l0y - 2 * l0x * l0x;

            coefs.tDiff_t5 = (6 * p3z * p3z + (-36 * p2z + 36 * p1z - 12 * p0z) * p3z + 6 * p3y * p3y + (-36 * p2y + 36 * p1y - 12 * p0y) * p3y + 6 * p3x * p3x + (-36 * p2x + 36 * p1x - 12 * p0x) * p3x + 54 * p2z * p2z + (-108 * p1z + 36 * p0z) * p2z + 54 * p2y * p2y + (-108 * p1y + 36 * p0y) * p2y + 54 * p2x * p2x + (-108 * p1x + 36 * p0x) * p2x + 54 * p1z * p1z - 36 * p0z * p1z + 54 * p1y * p1y - 36 * p0y * p1y + 54 * p1x * p1x - 36 * p0x * p1x + 6 * p0z * p0z + 6 * p0y * p0y + 6 * p0x * p0x);
            coefs.tDiff_t4 = ((30 * p2z - 60 * p1z + 30 * p0z) * p3z + (30 * p2y - 60 * p1y + 30 * p0y) * p3y + (30 * p2x - 60 * p1x + 30 * p0x) * p3x - 90 * p2z * p2z + (270 * p1z - 120 * p0z) * p2z - 90 * p2y * p2y + (270 * p1y - 120 * p0y) * p2y - 90 * p2x * p2x + (270 * p1x - 120 * p0x) * p2x - 180 * p1z * p1z + 150 * p0z * p1z - 180 * p1y * p1y + 150 * p0y * p1y - 180 * p1x * p1x + 150 * p0x * p1x - 30 * p0z * p0z - 30 * p0y * p0y - 30 * p0x * p0x);
            coefs.tDiff_t3 = ((24 * p1z - 24 * p0z) * p3z + (24 * p1y - 24 * p0y) * p3y + (24 * p1x - 24 * p0x) * p3x + 36 * p2z * p2z + (-216 * p1z + 144 * p0z) * p2z + 36 * p2y * p2y + (-216 * p1y + 144 * p0y) * p2y + 36 * p2x * p2x + (-216 * p1x + 144 * p0x) * p2x + 216 * p1z * p1z - 240 * p0z * p1z + 216 * p1y * p1y - 240 * p0y * p1y + 216 * p1x * p1x - 240 * p0x * p1x + 60 * p0z * p0z + 60 * p0y * p0y + 60 * p0x * p0x);
            coefs.tDiff_t2 = ((6 * p0z - 6 * l0z) * p3z + (6 * p0y - 6 * l0y) * p3y + (6 * p0x - 6 * l0x) * p3x + (54 * p1z - 72 * p0z + 18 * l0z) * p2z + (54 * p1y - 72 * p0y + 18 * l0y) * p2y + (54 * p1x - 72 * p0x + 18 * l0x) * p2x - 108 * p1z * p1z + (180 * p0z - 18 * l0z) * p1z - 108 * p1y * p1y + (180 * p0y - 18 * l0y) * p1y - 108 * p1x * p1x + (180 * p0x - 18 * l0x) * p1x - 60 * p0z * p0z + 6 * l0z * p0z - 60 * p0y * p0y + 6 * l0y * p0y - 60 * p0x * p0x + 6 * l0x * p0x);
            coefs.tDiff_t = ((12 * p0z - 12 * l0z) * p2z + (12 * p0y - 12 * l0y) * p2y + (12 * p0x - 12 * l0x) * p2x + 18 * p1z * p1z + (-60 * p0z + 24 * l0z) * p1z + 18 * p1y * p1y + (-60 * p0y + 24 * l0y) * p1y + 18 * p1x * p1x + (-60 * p0x + 24 * l0x) * p1x + 30 * p0z * p0z - 12 * l0z * p0z + 30 * p0y * p0y - 12 * l0y * p0y + 30 * p0x * p0x - 12 * l0x * p0x);
            coefs.tDiff_stt = ((-6 * l1z + 6 * l0z) * p3z + (-6 * l1y + 6 * l0y) * p3y + (-6 * l1x + 6 * l0x) * p3x + (18 * l1z - 18 * l0z) * p2z + (18 * l1y - 18 * l0y) * p2y + (18 * l1x - 18 * l0x) * p2x + (-18 * l1z + 18 * l0z) * p1z + (-18 * l1y + 18 * l0y) * p1y + (-18 * l1x + 18 * l0x) * p1x + (6 * l1z - 6 * l0z) * p0z + (6 * l1y - 6 * l0y) * p0y + (6 * l1x - 6 * l0x) * p0x);
            coefs.tDiff_st = ((-12 * l1z + 12 * l0z) * p2z + (-12 * l1y + 12 * l0y) * p2y + (-12 * l1x + 12 * l0x) * p2x + (24 * l1z - 24 * l0z) * p1z + (24 * l1y - 24 * l0y) * p1y + (24 * l1x - 24 * l0x) * p1x + (-12 * l1z + 12 * l0z) * p0z + (-12 * l1y + 12 * l0y) * p0y + (-12 * l1x + 12 * l0x) * p0x);
            coefs.tDiff_s = ((-6 * l1z + 6 * l0z) * p1z + (-6 * l1y + 6 * l0y) * p1y + (-6 * l1x + 6 * l0x) * p1x + (6 * l1z - 6 * l0z) * p0z + (6 * l1y - 6 * l0y) * p0y + (6 * l1x - 6 * l0x) * p0x);
            coefs.tDiff_constant = (6 * p0z - 6 * l0z) * p1z + (6 * p0y - 6 * l0y) * p1y + (6 * p0x - 6 * l0x) * p1x - 6 * p0z * p0z + 6 * l0z * p0z - 6 * p0y * p0y + 6 * l0y * p0y - 6 * p0x * p0x + 6 * l0x * p0x;

            return coefs;
        }
        private struct Matrix2x2
        {
            public double m00;
            public double m01;
            public double m10;
            public double m11;
            public void Inverse()
            {
                double denom = (m00 * m11 - m01 * m10);
                double new_m00 = m11/denom;
                double new_m01 = -m01/denom;
                double new_m10 = -m10/denom;
                double new_m11 = m00/denom;
                m00 = new_m00;
                m01 = new_m01;
                m10 = new_m10;
                m11 = new_m11;
            }
            //[m00 m01][x] = [x*m00+y*m01]
            //[m10 m11][y]   [x*m10+y*m11]
            public static Vec2 operator *(Matrix2x2 mat, Vec2 vec)
            {
                Vec2 retr = new Vec2();
                retr.x = vec.x * mat.m00 + vec.y * mat.m01;
                retr.y = vec.x * mat.m10 + vec.y * mat.m11;
                return retr;
            }
        }
        private struct Vec2
        {
            public double x;
            public double y;

            public static Vec2 operator -(Vec2 a, Vec2 b)
            {
                Vec2 retr = new Vec2();
                retr.x = a.x - b.x;
                retr.y = a.y - b.y;
                return retr;
            }
        }
        private struct Coefs 
        {
            public double hessianUpperLeft_constant;

            public double hessianDiagonal_constant;
            public double hessianDiagonal_t;
            public double hessianDiagonal_t2;

            public double hessianLowerRight_constant;
            public double hessianLowerRight_t;
            public double hessianLowerRight_t2;
            public double hessianLowerRight_t3;
            public double hessianLowerRight_t4;
            public double hessianLowerRight_s;
            public double hessianLowerRight_st;

            public double sDiff_constant;
            public double sDiff_t;
            public double sDiff_t2;
            public double sDiff_t3;
            public double sDiff_s;

            public double tDiff_constant;
            public double tDiff_t;
            public double tDiff_t2;
            public double tDiff_t3;
            public double tDiff_t4;
            public double tDiff_t5;
            public double tDiff_s;
            public double tDiff_st;
            public double tDiff_stt;
        }
    }
}
