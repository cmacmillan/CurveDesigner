using Assets.NewUI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MeshGenerator;

public partial class BezierCurve
{
    private static Vector3 NormalTangent(Vector3 forwardVector, Vector3 previous)
    {
        return Vector3.ProjectOnPlane(previous, forwardVector).normalized;
    }
    public static int mod(int x, int m)
    {
        return (x % m + m) % m;
    }
    public MeshIndexRing CreateRingPointsAlongCurve(ref int pointIndex, PointOnCurve currentPoint, List<Vector3> listToAppend, IDistanceSampler<float> sizeSampler, float TubeArc, float TubeThickness, int RingPointCount, IDistanceSampler<float> rotationSampler, bool isExterior, bool isClosedLoop, float DefaultSize, float DefaultRotation, float curveLength)
    {
        float distanceFromFull = 360.0f - TubeArc;
        float offset = (isExterior ? .5f : -.5f) * (TubeThickness);
        var size = Mathf.Max(0, sizeSampler.GetValueAtDistance(currentPoint.distanceFromStartOfCurve, isClosedLoop, curveLength, this) + offset + DefaultSize);
        var rotation = rotationSampler.GetValueAtDistance(currentPoint.distanceFromStartOfCurve, isClosedLoop, curveLength, this) + DefaultRotation;
        var ring = new MeshIndexRing();
        float rotationStepSize = TubeArc / (RingPointCount - (TubeArc == 360.0 ? 0.0f : 1.0f));
        for (int j = 0; j < RingPointCount; j++)
        {
            float theta = (j*rotationStepSize) + distanceFromFull / 2 + rotation;
            Vector3 rotatedVect = Quaternion.AngleAxis(theta, currentPoint.tangent) * currentPoint.reference;
            ring.points.Add(new MeshIndexRingPoint(theta, pointIndex));
            listToAppend.Add(currentPoint.GetRingPoint(theta, size));
            pointIndex++;
        }
        ring.minTheta = distanceFromFull / 2 + rotation;
        ring.maxTheta = ring.minTheta + TubeArc;
        return ring;
    } 
    public void CreateRingPointsAlongCurve(List<PointOnCurve> points, List<Vector3> listToAppend, IDistanceSampler<float> sizeSampler, float TubeArc, float TubeThickness, int RingPointCount, IDistanceSampler<float> rotationSampler, bool isExterior, bool isClosedLoop,float DefaultSize, float DefaultRotation, float curveLength)
    {
        float distanceFromFull = 360.0f - TubeArc;
        for (int i = 0; i < points.Count; i++)
        {
            PointOnCurve currentPoint = points[i];
            float offset = (isExterior ? .5f : -.5f) * (TubeThickness);
            var size = Mathf.Max(0, sizeSampler.GetValueAtDistance(currentPoint.distanceFromStartOfCurve,isClosedLoop,curveLength,this) + offset+DefaultSize);
            var rotation = rotationSampler.GetValueAtDistance(currentPoint.distanceFromStartOfCurve, isClosedLoop, curveLength,this)+DefaultRotation;
            for (int j = 0; j < RingPointCount; j++)
            {
                float theta = (TubeArc * j / (RingPointCount - (TubeArc == 360.0 ? 0 : 1))) + distanceFromFull / 2 + rotation;
                Vector3 rotatedVect = Quaternion.AngleAxis(theta, currentPoint.tangent) * currentPoint.reference;
                listToAppend.Add(currentPoint.GetRingPoint(theta,size));
            }
        }
    }
    public void CreateRectanglePointsAlongCurve(List<PointOnCurve> points, List<Vector3> listToAppend, IDistanceSampler<float> rotationSampler, bool isClosedLoop, float thickness, IDistanceSampler<float> distanceSampler,float DefaultSize,float DefaultRotation, float curveLength)
    {
        for (int i = 0; i < points.Count; i++)
        {
            PointOnCurve currentPoint = points[i];
            var center = currentPoint.position;
            var rotation = rotationSampler.GetValueAtDistance(currentPoint.distanceFromStartOfCurve, isClosedLoop, curveLength,this)+DefaultRotation;
            var up = Quaternion.AngleAxis(rotation,currentPoint.tangent)*currentPoint.reference.normalized;
            var right = Vector3.Cross(up, currentPoint.tangent).normalized;
            var scaledUp = up * thickness / 2.0f;
            var scaledRight = right*Mathf.Max(0,distanceSampler.GetValueAtDistance(currentPoint.distanceFromStartOfCurve,isClosedLoop,curveLength,this)+DefaultSize);
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
