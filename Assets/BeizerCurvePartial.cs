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
    public void CreateRingPointsAlongCurve(List<PointOnCurve> points, List<Vector3> listToAppend, IEvaluatable sizeCurve, float TubeArc, float TubeThickness, int RingPointCount, float Rotation, bool isExterior)
    {
        float distanceFromFull = 360.0f - TubeArc;
        void GenerateRing(PointOnCurve startPoint, Vector3 forwardVector, ref Vector3 previousTangent)
        {
            //Old Method: 
            //Vector3 tangentVect = NormalTangent(forwardVector, previousTangent);
            Vector3 tangentVect = NormalTangent(forwardVector, Vector3.up);
            previousTangent = tangentVect;
            float offset = (isExterior ? .5f : -.5f) * (TubeThickness);
            var size = Mathf.Max(0, sizeCurve.Evaluate(startPoint.distanceFromStartOfCurve) + offset);
            for (int j = 0; j < RingPointCount; j++)
            {
                float theta = (TubeArc * j / (RingPointCount - (TubeArc == 360.0 ? 0 : 1))) + distanceFromFull / 2 + Rotation;
                Vector3 rotatedVect = Quaternion.AngleAxis(theta, forwardVector) * tangentVect;
                listToAppend.Add(startPoint.position + rotatedVect * size);
            }
        }
        Vector3 lastTangent = Quaternion.FromToRotation(Vector3.forward, (points[1].position - points[0].position).normalized) * Vector3.right;
        for (int i = 0; i < points.Count - 1; i++)
        {
            GenerateRing(points[i], (points[i + 1].position - points[i].position).normalized, ref lastTangent);
        }
        int finalIndex = points.Count - 1;
        GenerateRing(points[finalIndex], (points[finalIndex].position - points[finalIndex - 1].position).normalized, ref lastTangent);
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
    public LinearEvaluatable(float startValue, float endValue)
    {
        _startValue = startValue;
        _endValue = endValue;
    }
    public float Evaluate(float time)
    {
        return Mathf.Lerp(_startValue, _endValue, time);
    }
}

public interface IEvaluatable
{
    float Evaluate(float time);
}
