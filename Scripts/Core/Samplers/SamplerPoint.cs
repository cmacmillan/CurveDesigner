using System;
using System.Collections.Generic;
using UnityEditor;

namespace ChaseMacMillan.CurveDesigner
{
    [System.Serializable]//Despite this class being generic, it can still serialize when inside a list
    public sealed class SamplerPoint<T> : ISelectEditable<SamplerPoint<T>>, ISamplerPoint
    {
        public T value;
        public int segmentIndex;
        public float time;
        public KeyframeInterpolationMode interpolationMode = KeyframeInterpolationMode.Linear;
        public SelectableGUID guid;
        [NonSerialized]
        public Sampler<T> owner;
        public KeyframeInterpolationMode InterpolationMode { get => interpolationMode; set => interpolationMode = value; }
        public SelectableGUID GUID { get { return guid; } set { guid = value; } }
        public float Time { get => time; set => time = value; }
        public int SegmentIndex { get => segmentIndex; set => segmentIndex = value; }

        public SamplerPoint(Sampler<T> owner,Curve3D curve) {
            this.owner = owner;
            GUID = curve.guidFactory.GetGUID(this);
        }
        public SamplerPoint(SamplerPoint<T> other, ISampler<T> owner, bool createNewGuids,Curve3D curve)
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

        public float GetDistance(BezierCurve positionCurve)
        {
            return positionCurve.GetDistanceAtSegmentIndexAndTime(segmentIndex,time);
        }

        public bool IsInsideVisibleCurve(BezierCurve curve)
        {
            return SegmentIndex < curve.NumSegments;
        }

        public void SelectEdit(Curve3D curve, List<SamplerPoint<T>> selectedPoints)
        {
            owner.SelectEdit(curve, selectedPoints,selectedPoints[0]);
        }
    }
}
