using System;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    /// <summary>
    /// <para>A Sampler represents a series of points of type T, each of which can be positioned at a particular point along a Curve3D</para>
    /// <para>You can sample at any distance along the curve and will recieve a value calculated by interpolating between the two adjacent points</para>
    /// </summary>
    /// <typeparam name="T">The type of points in the Sampler</typeparam>
    public abstract class ValueSampler<T> : Sampler<T>, IValueSampler<T>
    {
        public T constValue;

        [SerializeField]
        private bool _useKeyframes;
        public bool UseKeyframes { get => _useKeyframes; set => _useKeyframes = value; }
        public T ConstValue { get => constValue; set => constValue = value; }

        public abstract T Lerp(T val1, T val2, float lerp);
        public ValueSampler(string label,Curve3DEditMode editMode) : base(label,editMode)
        {
        }
        public ValueSampler(ValueSampler<T> objToClone,bool createNewGuids,Curve3D curve) : base(objToClone,createNewGuids,curve) {
            _useKeyframes = objToClone._useKeyframes;
            constValue = CloneValue(objToClone.constValue,createNewGuids);
        }
        protected override T GetInterpolatedValueAtDistance(float distance, BezierCurve curve)
        {
            return GetValueAtDistance(distance,curve);
        }
        public T GetValueAtDistance(float distance, BezierCurve curve)
        {
            float curveLength = curve.GetLength();
            bool isClosedLoop = curve.isClosedLoop;
            var pointsInsideCurve = GetPoints(curve);
            if (!_useKeyframes || pointsInsideCurve.Count == 0)
            {
                return constValue;
            }
            var firstPoint = pointsInsideCurve[0];
            var lastPoint = pointsInsideCurve[pointsInsideCurve.Count - 1];
            var lastDistance = curveLength - lastPoint.GetDistance(curve);
            float endSegmentDistance = firstPoint.GetDistance(curve) + lastDistance;
            if (pointsInsideCurve[0].GetDistance(curve) >= distance)
            {
                if (isClosedLoop && lastPoint.InterpolationMode == KeyframeInterpolationMode.Linear)
                {
                    float lerpVal = (lastDistance + distance) / endSegmentDistance;
                    return Lerp(lastPoint.value, firstPoint.value, lerpVal);
                }
                else
                    return pointsInsideCurve[0].value;
            }
            var previous = pointsInsideCurve[0];
            for (int i = 1; i < pointsInsideCurve.Count; i++)
            {
                var current = pointsInsideCurve[i];
                if (current.GetDistance(curve) >= distance)
                {
                    if (previous.InterpolationMode == KeyframeInterpolationMode.Linear)
                        return Lerp(previous.value, current.value, (distance - previous.GetDistance(curve)) / (current.GetDistance(curve) - previous.GetDistance(curve)));
                    else
                        return previous.value;
                }
                previous = current;
            }
            if (isClosedLoop && lastPoint.InterpolationMode == KeyframeInterpolationMode.Linear)
            {
                float lerpVal = (distance - lastPoint.GetDistance(curve)) / endSegmentDistance;
                return Lerp(lastPoint.value, firstPoint.value, lerpVal);
            }
            else
                return pointsInsideCurve[pointsInsideCurve.Count - 1].value;
        }
    }
}
