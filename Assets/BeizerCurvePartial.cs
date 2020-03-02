using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class BeizerCurve
{
    private static Vector3 GetArbitraryOrthoVector(Vector3 vect)//I think this does the same thing
    {
        if (vect != Vector3.right)
            return Vector3.Cross(Vector3.right, vect).normalized;
        return Vector3.Cross(Vector3.up, vect).normalized;
    }
    private Vector3 NormalTangent(Vector3 forwardVector, Vector3 previous)//as this, but maybe this one is slower
    {
        return Vector3.ProjectOnPlane(previous, forwardVector).normalized;
    }
    int mod(int x, int m)
    {
        return (x % m + m) % m;
    }
    public void CreateRingPointsAlongCurve(List<PointOnCurve> points, List<Vector3> listToAppend, IEvaluatable sizeCurve, float TubeArc, float TubeThickness, int RingPointCount, float Rotation, bool isExterior,Vector3? referenceDirection, bool isClosedLoop)
    {
        Vector3 GetReference(PointOnCurve currentPoint, PointOnCurve previousPoint, Vector3 previousReference)
        {
            //Double reflection method for calculating RMF
            Vector3 v1 = currentPoint.position - previousPoint.position;
            float c1 = Vector3.Dot(v1, v1);
            Vector3 rl = previousReference - (2.0f / c1) * Vector3.Dot(v1, previousReference) * v1;
            Vector3 tl = previousPoint.tangent.normalized - (2.0f / c1) * Vector3.Dot(v1, previousPoint.tangent.normalized) * v1;
            Vector3 v2 = currentPoint.tangent.normalized - tl;
            float c2 = Vector3.Dot(v2, v2);
            return rl - (2.0f / c2) * Vector3.Dot(v2, rl) * v2;
        }
        float distanceFromFull = 360.0f - TubeArc;
        void GenerateRing(PointOnCurve currentPoint, Vector3 referenceVector)
        {
            float offset = (isExterior ? .5f : -.5f) * (TubeThickness);
            var size = Mathf.Max(0, sizeCurve.Evaluate(currentPoint.distanceFromStartOfCurve) + offset);
            for (int j = 0; j < RingPointCount; j++)
            {
                float theta = (TubeArc * j / (RingPointCount - (TubeArc == 360.0 ? 0 : 1))) + distanceFromFull / 2 + Rotation;
                Vector3 rotatedVect = Quaternion.AngleAxis(theta, currentPoint.tangent) * referenceVector;
                listToAppend.Add(currentPoint.position + rotatedVect * size);
            }
        }
        List<Vector3> referenceVectors = new List<Vector3>(points.Count);
        {
            Vector3 referenceVector = NormalTangent(points[0].tangent, Vector3.up);
            referenceVector = referenceVector.normalized;
            referenceVectors.Add(referenceVector);
            for (int i = 1; i < points.Count; i++)
            {
                referenceVector = GetReference(points[i], points[i - 1], referenceVector).normalized;
                referenceVectors.Add(referenceVector);
            }
        }

        if (isClosedLoop)
        {
            float angleDifference = Vector3.SignedAngle(referenceVectors[points.Count-1],referenceVectors[0],points[0].tangent);
            for (int i = 1; i < points.Count; i++)
                referenceVectors[i] = Quaternion.AngleAxis((i/(float)(points.Count-1))*angleDifference,points[i].tangent) *referenceVectors[i];
        }
        for (int i = 0; i < points.Count; i++)
            GenerateRing(points[i], referenceVectors[i]);
    }
}
public class AnimationCurveIEvaluatableAdapter : IEvaluatable
{
    private AnimationCurve _curve;
    public AnimationCurveIEvaluatableAdapter(AnimationCurve curve)
    {
        _curve = curve;
    }
    public float Evaluate(float time)
    {
        return _curve.Evaluate(time);
    }
}

public class LinearEvaluatable : IEvaluatable
{
    private float _startValue;
    private float _endValue;
    private float _length;
    private float _base;
    public LinearEvaluatable(Vector2 start, Vector2 end)
    {
        _startValue = start.y;
        _endValue = end.y;
        _length = end.x-start.x;
        _base = start.x;
    }
    public float Evaluate(float time)
    {
        return Mathf.Lerp(_startValue, _endValue, (time-_base)/_length);
    }
}

public interface IEvaluatable
{
    float Evaluate(float time);
}
