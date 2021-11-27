using System;
using System.Collections.Generic;
using UnityEditor;

namespace ChaseMacMillan.CurveDesigner
{
    public class SamplerPoint<DataType,SamplerPointType> : ISelectEditable<SamplerPointType>, ISamplerPoint<DataType,SamplerPointType> where SamplerPointType : class, ISamplerPoint<DataType,SamplerPointType>, new()
    {
        public DataType value;
        public int segmentIndex;
        [NonSerialized]
        public float cachedDistance;
        public float time;
        public KeyframeInterpolationMode interpolationMode = KeyframeInterpolationMode.Linear;
        public SelectableGUID guid;
        [NonSerialized]
        public Sampler<DataType,SamplerPointType> owner;
        public KeyframeInterpolationMode InterpolationMode { get => interpolationMode; set => interpolationMode = value; }
        public SelectableGUID GUID { get { return guid; } set { guid = value; } }
        public float Time { get => time; set => time = value; }
        public int SegmentIndex { get => segmentIndex; set => segmentIndex = value; }
        public DataType Value { get => value; set => this.value = value; }
        public ISampler<DataType,SamplerPointType> Owner { get => owner; set => owner=(Sampler<DataType,SamplerPointType>)value; }
        public float CachedDistance { get => cachedDistance; set => cachedDistance = value; }

        public SamplerPoint() { }

        public void Construct(ISampler<DataType,SamplerPointType> owner,Curve3D curve) {
            this.owner = (Sampler<DataType,SamplerPointType>)owner;
            GUID = curve.guidFactory.GetGUID(this);
        }
        public void Construct(ISamplerPoint<DataType,SamplerPointType> other, ISampler<DataType,SamplerPointType> owner, bool createNewGuids,Curve3D curve)
        {
            value = owner.CloneValue(other.Value,createNewGuids);
            segmentIndex = other.SegmentIndex;
            time = other.Time;
            interpolationMode = other.InterpolationMode;
            if (createNewGuids)
                guid = curve.guidFactory.GetGUID(this);
            else
                guid = SelectableGUID.Null;
        }

        public void SetDistance(float distance, BezierCurve curve, bool shouldSort = true)
        {
            var point = curve.GetPointAtDistance(distance);
            segmentIndex = point.segmentIndex;
            time = point.time;
            if (shouldSort)
                owner.Sort(curve);
        }

        public float GetDistance(BezierCurve positionCurve, bool useCachedDistance=false)
        {
            if (useCachedDistance)
                return cachedDistance;
            else
                return positionCurve.GetDistanceAtSegmentIndexAndTime(segmentIndex, time);
        }

        public bool IsInsideVisibleCurve(BezierCurve curve)
        {
            return SegmentIndex < curve.NumSegments;
        }

#if UNITY_EDITOR
        public void SelectEdit(Curve3D curve, List<SamplerPointType> selectedPoints)
        {
            owner.SelectEdit(curve, selectedPoints,selectedPoints[0]);
        }
#endif

        public float GetDistance(BezierCurve positionCurve)
        {
            return GetDistance(positionCurve, false);
        }
    }
}
