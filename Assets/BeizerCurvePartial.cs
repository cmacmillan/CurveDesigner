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
    public void CreateRingPointsAlongCurve(List<PointOnCurve> points, List<Vector3> listToAppend, IEvaluatable sizeCurve, float TubeArc, float TubeThickness, int RingPointCount, float Rotation, bool isExterior, bool isClosedLoop)
    {
        float distanceFromFull = 360.0f - TubeArc;
        for (int i = 0; i < points.Count; i++)
        {
            PointOnCurve currentPoint = points[i];
            float offset = (isExterior ? .5f : -.5f) * (TubeThickness);
            var size = Mathf.Max(0, sizeCurve.Evaluate(currentPoint.distanceFromStartOfCurve) + offset)/2.0f;
            for (int j = 0; j < RingPointCount; j++)
            {
                float theta = (TubeArc * j / (RingPointCount - (TubeArc == 360.0 ? 0 : 1))) + distanceFromFull / 2 + Rotation;
                Vector3 rotatedVect = Quaternion.AngleAxis(theta, currentPoint.tangent) * currentPoint.reference;
                listToAppend.Add(currentPoint.position + rotatedVect * size);
            }
        }
    }
    public void CreateRectanglePointsAlongCurve(List<PointOnCurve> points, List<Vector3> listToAppend, float Rotation, bool isClosedLoop, float thickness, IEvaluatable sizeCurve)
    {
        for (int i = 0; i < points.Count; i++)
        {
            PointOnCurve currentPoint = points[i];
            var center = currentPoint.position;
            var up = Quaternion.AngleAxis(Rotation,currentPoint.tangent)*currentPoint.reference.normalized;
            var right = Vector3.Cross(up, currentPoint.tangent).normalized;
            var scaledUp = up * thickness / 2.0f;
            var scaledRight = right*Mathf.Max(0,sizeCurve.Evaluate(currentPoint.distanceFromStartOfCurve))/2.0f;
            listToAppend.Add(center+scaledUp+scaledRight);
            listToAppend.Add(center+scaledUp-scaledRight);
            listToAppend.Add(center-scaledUp-scaledRight);
            listToAppend.Add(center-scaledUp+scaledRight);
        }
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
