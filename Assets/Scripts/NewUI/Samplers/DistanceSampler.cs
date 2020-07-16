using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.NewUI
{
    //should be using dependency injection for this, but with all these generics that'd be a total nightmare because the injected objects wouldn't serialize unless wrapped in yet another class lol
    public abstract class FieldEditableSamplerPoint<T,S,Q> : SamplerPoint<T,S,Q> where Q : DistanceSampler<T,S,Q> where S : FieldEditableSamplerPoint<T, S, Q>, new()
    {
        public abstract T Field(string displayName, T originalValue);
        public abstract T Subtract(T v1, T v2);
        public abstract T Add(T v1, T v2);
        public abstract T Zero();
        public override bool SelectEdit(Curve3D curve, List<S> selectedPoints)
        {
            T originalValue = value;
            T fieldVal = Field(owner.fieldDisplayName, originalValue);
            T valueOffset = Subtract(fieldVal,originalValue);
            base.SelectEdit(curve, selectedPoints);
            if (valueOffset.Equals(Zero()))
                return false;
            foreach (var target in selectedPoints)
                target.value = Add(target.value,valueOffset);
            return true;
        }
    }

    [System.Serializable]
    public class SamplerPoint<T,S,Q> : ISelectEditable<S>, ISamplerPoint where Q : DistanceSampler<T,S,Q> where S : SamplerPoint<T,S,Q>, new()
    {
        public T value;
        [NonSerialized]
        public Q owner;
        public int segmentIndex;
        public float time;

        private SelectableGUID _guid;
        public SelectableGUID GUID { get { return _guid; } set { _guid = value; } }

        public float Time { get => time; set => time = value; }
        public int SegmentIndex { get => segmentIndex; set => segmentIndex = value; }

        public virtual bool SelectEdit(Curve3D curve, List<S> selectedPoints)
        {
            float originalDistance = GetDistance(curve.positionCurve);
            float distanceOffset = EditorGUILayout.FloatField("Distance along curve", originalDistance) - originalDistance;
            if (distanceOffset == 0)
                return false;
            foreach (var target in selectedPoints)
            {
                var ogDistance = target.GetDistance(curve.positionCurve);
                target.SetDistance(ogDistance + distanceOffset, curve.positionCurve);
            }
            return true;
        }

        public void SetDistance(float distance,BezierCurve curve, bool shouldSort = true)
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
    }

    public abstract class ValueDistanceSampler<T,S,Q> : DistanceSampler<T, S, Q> where Q : ValueDistanceSampler<T,S,Q> where S : SamplerPoint<T,S,Q>, new()
    {
        public string fieldEditDisplayName = "";
        public string FieldEditDisplayName => fieldEditDisplayName;
        public abstract T GetDefaultVal();
        public abstract T Lerp(T val1, T val2, float lerp);
        protected override T GetInterpolatedValueAtDistance(float distance, BezierCurve curve)
        {
            return GetValueAtDistance(distance, curve.isClosedLoop, curve.GetLength(),curve);
        }
        public T GetValueAtDistance(float distance, bool isClosedLoop, float curveLength, BezierCurve curve)
        {
            var pointsInsideCurve = GetPoints(curve);
            if (pointsInsideCurve.Count == 0)
                return GetDefaultVal();
            var firstPoint = pointsInsideCurve[0];
            var lastPoint = pointsInsideCurve[pointsInsideCurve.Count - 1];
            var lastDistance = curveLength - lastPoint.GetDistance(curve);
            float endSegmentDistance = firstPoint.GetDistance(curve) + lastDistance;
            if (pointsInsideCurve[0].GetDistance(curve) >= distance)
                if (isClosedLoop)
                {
                    float lerpVal = (lastDistance + distance) / endSegmentDistance;
                    return Lerp(lastPoint.value, firstPoint.value, lerpVal);
                }
                else
                    return pointsInsideCurve[0].value;
            var previous = pointsInsideCurve[0];
            for (int i = 1; i < pointsInsideCurve.Count; i++)
            {
                var current = pointsInsideCurve[i];
                if (current.GetDistance(curve) >= distance)
                    return Lerp(previous.value, current.value, (distance - previous.GetDistance(curve)) / (current.GetDistance(curve) - previous.GetDistance(curve)));
                previous = current;
            }
            if (isClosedLoop)
            {
                float lerpVal = (distance - lastPoint.GetDistance(curve)) / endSegmentDistance;
                return Lerp(lastPoint.value, firstPoint.value, lerpVal);
            }
            else
                return pointsInsideCurve[pointsInsideCurve.Count - 1].value;
        }
    }

    [System.Serializable]
    public abstract class DistanceSampler<T,S,Q> : ISerializationCallbackReceiver, IDistanceSampler where Q : DistanceSampler<T,S,Q> where S : SamplerPoint<T,S,Q>, new()
    {
        public List<S> points = new List<S>();

        [NonSerialized]
        private List<S> points_openCurveOnly = null;

        public string fieldDisplayName="";

        public IEnumerable<ISamplerPoint> GetPoints()
        {
            return points;
        }

        protected abstract T GetInterpolatedValueAtDistance(float distance, BezierCurve curve);

        public int InsertPointAtDistance(float distance, BezierCurve curve) {
            T interpolatedValue = GetInterpolatedValueAtDistance(distance, curve);
            var newPoint = new S();
            newPoint.GUID = curve.owner.guidFactory.GetGUID();
            newPoint.value = interpolatedValue;
            newPoint.owner = this as Q;
            newPoint.SetDistance(distance,curve);
            points.Add(newPoint);
            return points.IndexOf(newPoint);
        }

        public List<S> GetPoints(BezierCurve curve)
        {
            if (curve.isClosedLoop)
                return points;
            if (points_openCurveOnly == null)
                RecalculateOpenCurveOnlyPoints(curve);
            return points_openCurveOnly;
        }

        /// <summary>
        /// Should be called whenever this sampler is sorted, when a point in this sampler is moved/inserted (which should trigger a sort), or after deserialization
        /// </summary>
        public void RecalculateOpenCurveOnlyPoints(BezierCurve curve)
        {
            points_openCurveOnly = new List<S>();
            foreach (var i in points)
                if (i.segmentIndex < curve.NumSegments)
                    points_openCurveOnly.Add(i);
        }

        public void Sort(BezierCurve curve)
        {
            points = points.OrderBy((a) => a.time).OrderBy(a=>a.segmentIndex).ToList();
            RecalculateOpenCurveOnlyPoints(curve);
        }

        public void OnBeforeSerialize() { /*Do Nothing*/ }

        public void OnAfterDeserialize()
        {
            foreach (var i in points)
                i.owner = this as Q;
        }
    }
    public interface ISamplerPoint : ISelectable
    {
        float Time { get; set; }
        int SegmentIndex { get; set; }
        void SetDistance(float distance,BezierCurve curve,bool shouldSort);
    }
    public interface IDistanceSampler
    {
        IEnumerable<ISamplerPoint> GetPoints();
        void RecalculateOpenCurveOnlyPoints(BezierCurve curve);
        void Sort(BezierCurve curve);
        int InsertPointAtDistance(float distance,BezierCurve curve);
    }
}
