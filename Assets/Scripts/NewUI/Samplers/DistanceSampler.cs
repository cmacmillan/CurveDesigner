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
    [System.Serializable]
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
    public abstract class SamplerPoint<T,S,Q> : ISelectEditable<S>, ISamplerPoint where Q : DistanceSampler<T,S,Q> where S : SamplerPoint<T,S,Q>, new()
    {
        public T value;
        [NonSerialized]
        public Q owner;
        public int segmentIndex;
        public float time;

        public abstract T CloneValue(T value);

        public void Copy(SamplerPoint<T,S,Q> objToClone, DistanceSampler<T,S,Q> newOwner)
        {
            value = CloneValue(objToClone.value);
            owner = newOwner as Q;
            segmentIndex = objToClone.segmentIndex;
            time = objToClone.time;
        }

        [SerializeField]
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
            distanceOffset = PointOnCurveClickCommand.ClampOffset(distanceOffset, curve,selectedPoints);
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

    [System.Serializable]
    public abstract class ValueDistanceSampler<T,S,Q> : DistanceSampler<T, S, Q> where Q : ValueDistanceSampler<T,S,Q> where S : SamplerPoint<T,S,Q>, new()
    {
        public abstract T GetDefaultVal();
        public abstract T Lerp(T val1, T val2, float lerp);
        public ValueDistanceSampler(string fieldDisplayName)
        {
            this.fieldDisplayName = fieldDisplayName;
        }
        public ValueDistanceSampler(ValueDistanceSampler<T,S,Q> objToClone) : base(objToClone) { }
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

        public DistanceSampler() { }

        public DistanceSampler(DistanceSampler<T,S,Q> objToClone)
        {
            fieldDisplayName = objToClone.fieldDisplayName;

            S Clone(S obj)
            {
                var clonedPoint = new S();
                clonedPoint.Copy(obj, this);
                return clonedPoint;
            }

            foreach (var i in objToClone.points)
                points.Add(Clone(i));

            points_openCurveOnly = new List<S>();

            foreach (var i in objToClone.points_openCurveOnly)
                points_openCurveOnly.Add(Clone(i));
        }

        public IEnumerable<ISamplerPoint> AllPoints()
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
            points.Add(newPoint);
            newPoint.SetDistance(distance,curve);
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
        /// Should be called whenever this sampler is sorted, when a point is deleted, when a point in this sampler is moved/inserted (which should trigger a sort), or after deserialization
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

        public ISelectable GetSelectable(int index, Curve3D curve)
        {
            return GetPoints(curve.positionCurve)[index];
        }

        public int NumSelectables(Curve3D curve)
        {
            return GetPoints(curve.positionCurve).Count;
        }

        public bool Delete(List<SelectableGUID> guids, Curve3D curve)
        {
            bool retr = SelectableGUID.Delete(ref points, guids, curve);
            if (retr)
                RecalculateOpenCurveOnlyPoints(curve.positionCurve);
            return retr;
        }

        public List<SelectableGUID> SelectAll(Curve3D curve)
        {
            List<SelectableGUID> retr = new List<SelectableGUID>();
            var points = GetPoints(curve.positionCurve);
            foreach (var i in points)
                retr.Add(i.GUID);
            return retr;
        }
    }
    public interface ISamplerPoint : ISelectable
    {
        float Time { get; set; }
        int SegmentIndex { get; set; }
        void SetDistance(float distance,BezierCurve curve,bool shouldSort=true);
    }
    public interface IDistanceSampler : IActiveElement
    {
        IEnumerable<ISamplerPoint> AllPoints();
        void RecalculateOpenCurveOnlyPoints(BezierCurve curve);
        void Sort(BezierCurve curve);
        int InsertPointAtDistance(float distance,BezierCurve curve);
    }
}
