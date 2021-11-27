using System;
using System.Collections.Generic;
using UnityEditor;

namespace ChaseMacMillan.CurveDesigner
{
    public class SamplerPoint<DataType> : ISelectEditable<SamplerPoint<DataType>>, ISamplerPoint
    {
        public DataType value;
        public int segmentIndex;
        [NonSerialized]
        public float cachedDistance;
        public float time;
        public KeyframeInterpolationMode interpolationMode = KeyframeInterpolationMode.Linear;
        public SelectableGUID guid;
        [NonSerialized]
        public Sampler<DataType,SamplerPoint<DataType>> owner;
        public KeyframeInterpolationMode InterpolationMode { get => interpolationMode; set => interpolationMode = value; }
        public SelectableGUID GUID { get { return guid; } set { guid = value; } }
        public float Time { get => time; set => time = value; }
        public int SegmentIndex { get => segmentIndex; set => segmentIndex = value; }

        public SamplerPoint(Sampler<DataType,SamplerPoint<DataType>> owner,Curve3D curve) {
            this.owner = owner;
            GUID = curve.guidFactory.GetGUID(this);
        }
        public SamplerPoint(ISamplerPoint<DataType> other, ISampler<DataType> owner, bool createNewGuids,Curve3D curve)
        {
            value = owner.CloneValue(other.value,createNewGuids);
            segmentIndex = other.segmentIndex;
            time = other.time;
            interpolationMode = other.interpolationMode;
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
        public void SelectEdit(Curve3D curve, List<SamplerPoint<DataType>> selectedPoints)
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
