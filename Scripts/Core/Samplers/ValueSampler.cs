using System;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    /// <summary>
    /// <para>A Sampler represents a series of points of type T, each of which can be positioned at a particular point along a Curve3D</para>
    /// <para>You can sample at any distance along the curve and will recieve a value calculated by interpolating between the two adjacent points</para>
    /// </summary>
    /// <typeparam name="T">The type of points in the Sampler</typeparam>
    public abstract class ValueSampler<DataType,SamplerPointType> : Sampler<DataType,SamplerPointType>, IValueSampler<DataType> where SamplerPointType : class, ISamplerPoint<DataType,SamplerPointType>, new()
    {
        public DataType constValue;

        [SerializeField]
        private bool _useKeyframes;
        public bool UseKeyframes { get => _useKeyframes; set => _useKeyframes = value; }
        public DataType ConstValue { get => constValue; set => constValue = value; }

        public abstract DataType Lerp(DataType val1, DataType val2, float lerp);
#if UNITY_EDITOR
        public abstract void ConstantField(Rect rect);
#endif
        public ValueSampler(string label,Curve3DEditMode editMode) : base(label,editMode)
        {
        }
        public ValueSampler(ValueSampler<DataType,SamplerPointType> objToClone,bool createNewGuids,Curve3D curve) : base(objToClone,createNewGuids,curve) {
            _useKeyframes = objToClone._useKeyframes;
            constValue = CloneValue(objToClone.constValue,createNewGuids);
        }
        protected override DataType GetInterpolatedValueAtDistance(float distance, BezierCurve curve)
        {
            return GetValueAtDistance(distance,curve);
        }
        public DataType GetValueAtDistance(float distance, BezierCurve curve, bool useCachedDistance=false)
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
            var lastDistance = curveLength - lastPoint.GetDistance(curve,useCachedDistance);
            float endSegmentDistance = firstPoint.GetDistance(curve,useCachedDistance) + lastDistance;
            if (pointsInsideCurve[0].GetDistance(curve,useCachedDistance) >= distance)
            {
                if (isClosedLoop && lastPoint.InterpolationMode == KeyframeInterpolationMode.Linear)
                {
                    float lerpVal = (lastDistance + distance) / endSegmentDistance;
                    return Lerp(lastPoint.Value, firstPoint.Value, lerpVal);
                }
                else
                    return pointsInsideCurve[0].Value;
            }
            var previous = pointsInsideCurve[0];
            for (int i = 1; i < pointsInsideCurve.Count; i++)
            {
                var current = pointsInsideCurve[i];
                if (current.GetDistance(curve,useCachedDistance) >= distance)
                {
                    if (previous.InterpolationMode == KeyframeInterpolationMode.Linear)
                        return Lerp(previous.Value, current.Value, (distance - previous.GetDistance(curve,useCachedDistance)) / (current.GetDistance(curve,useCachedDistance) - previous.GetDistance(curve,useCachedDistance)));
                    else
                        return previous.Value;
                }
                previous = current;
            }
            if (isClosedLoop && lastPoint.InterpolationMode == KeyframeInterpolationMode.Linear)
            {
                float lerpVal = (distance - lastPoint.GetDistance(curve,useCachedDistance)) / endSegmentDistance;
                return Lerp(lastPoint.Value, firstPoint.Value, lerpVal);
            }
            else
                return pointsInsideCurve[pointsInsideCurve.Count - 1].Value;
        }
    }
}
