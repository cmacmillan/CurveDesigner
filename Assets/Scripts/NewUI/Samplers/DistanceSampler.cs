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
            T fieldVal = Field(owner.GetLabel(), originalValue);
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

        [SerializeField]
        private InterpolationMode _interpolationMode = InterpolationMode.Flat;
        public InterpolationMode InterpolationMode { get => _interpolationMode; set => _interpolationMode= value; }

        public abstract T CloneValue(T value);

        public void Copy(SamplerPoint<T,S,Q> objToClone, DistanceSampler<T,S,Q> newOwner)
        {
            value = CloneValue(objToClone.value);
            owner = newOwner as Q;
            segmentIndex = objToClone.segmentIndex;
            time = objToClone.time;
            _interpolationMode =  objToClone._interpolationMode;
        }

        [SerializeField]
        private SelectableGUID _guid;
        public SelectableGUID GUID { get { return _guid; } set { _guid = value; } }

        public float Time { get => time; set => time = value; }
        public int SegmentIndex { get => segmentIndex; set => segmentIndex = value; }

        public bool IsInsideVisibleCurve(BezierCurve curve)
        {
            return SegmentIndex < curve.NumSegments;
        }

        public virtual bool SelectEdit(Curve3D curve, List<S> selectedPoints)
        {
            float originalDistance = GetDistance(curve.positionCurve);
            float distanceOffset = EditorGUILayout.FloatField("Distance along curve", originalDistance) - originalDistance;
            InterpolationMode = (InterpolationMode)EditorGUILayout.EnumPopup("Interpolation",InterpolationMode);
            if (distanceOffset == 0)
                return false;
            PointOnCurveClickCommand.ClampOffset(distanceOffset, curve,selectedPoints);
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
    public abstract class ValueDistanceSampler<T,S,Q> : DistanceSampler<T, S, Q>, IValueSampler<T> where Q : ValueDistanceSampler<T,S,Q> where S : SamplerPoint<T,S,Q>, new()
    {
        public T constValue;

        [SerializeField]
        private ValueType _valueType;

        public ValueType ValueType { get => _valueType; set => _valueType = value; }
        public T ConstValue { get => constValue; set => constValue = value; }

        protected abstract T CloneValue(T value);

        public abstract T Lerp(T val1, T val2, float lerp);
        public ValueDistanceSampler(string label,EditMode editMode) : base(label,editMode)
        {
        }
        public ValueDistanceSampler(ValueDistanceSampler<T,S,Q> objToClone) : base(objToClone) {
            _valueType = objToClone._valueType;
            constValue = CloneValue(objToClone.constValue);
        }
        protected override T GetInterpolatedValueAtDistance(float distance, BezierCurve curve)
        {
            return GetValueAtDistance(distance, curve.isClosedLoop, curve.GetLength(),curve);
        }
        public T GetValueAtDistance(float distance, bool isClosedLoop, float curveLength, BezierCurve curve)
        {
            var pointsInsideCurve = GetPoints(curve);
            if (_valueType == ValueType.Constant || pointsInsideCurve.Count == 0)
            {
                return constValue;
            }
            var firstPoint = pointsInsideCurve[0];
            var lastPoint = pointsInsideCurve[pointsInsideCurve.Count - 1];
            var lastDistance = curveLength - lastPoint.GetDistance(curve);
            float endSegmentDistance = firstPoint.GetDistance(curve) + lastDistance;
            if (pointsInsideCurve[0].GetDistance(curve) >= distance)
                if (isClosedLoop && lastPoint.InterpolationMode == InterpolationMode.Linear)
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
                {
                    if (previous.InterpolationMode == InterpolationMode.Linear)
                        return Lerp(previous.value, current.value, (distance - previous.GetDistance(curve)) / (current.GetDistance(curve) - previous.GetDistance(curve)));
                    else
                        return previous.value;
                }
                previous = current;
            }
            if (isClosedLoop && lastPoint.InterpolationMode == InterpolationMode.Linear)
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

        [SerializeField]
        private string label;
        [SerializeField]
        private EditMode editMode;
        public DistanceSampler(string label, EditMode editMode) {
            this.label = label;
            this.editMode = editMode;
        }

        public DistanceSampler(DistanceSampler<T,S,Q> objToClone)
        {
            S Clone(S obj)
            {
                var clonedPoint = new S();
                clonedPoint.Copy(obj, this);
                return clonedPoint;
            }

            this.label = objToClone.label;
            this.editMode = objToClone.editMode;

            foreach (var i in objToClone.points)
                points.Add(Clone(i));

            points_openCurveOnly = new List<S>();

            foreach (var i in objToClone.points_openCurveOnly)
                points_openCurveOnly.Add(Clone(i));
        }
        public string GetLabel()
        {
            return label;
        }

        public EditMode GetEditMode()
        {
            return editMode;
        }

        public IEnumerable<ISamplerPoint> AllPoints()
        {
            return points;
        }

        IEnumerable<ISamplerPoint> IDistanceSampler.GetPoints(BezierCurve curve)
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
            var valuePoint = newPoint as ISamplerPoint;
            if (valuePoint != null && TryGetPointBelowDistance(distance, curve, out S point))
                valuePoint.InterpolationMode = point.InterpolationMode;
            points.Add(newPoint);
            newPoint.SetDistance(distance,curve);
            return points.IndexOf(newPoint);
        }

        private bool TryGetPointBelowDistance(float distance, BezierCurve curve,out S point)
        {
            point = null;
            var points = GetPoints(curve);
            if (points.Count == 0)
                return false;
            if (distance < points[0].GetDistance(curve) && !curve.isClosedLoop){
                point = points[0];
                return true;
            }
            for (int i = 0; i < points.Count; i++)
            {
                var curr = points[i];
                if (curr.GetDistance(curve) > distance)
                {
                    point = points[i - 1];
                    return true;
                }
            }
            point = points.Last();
            return true;
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
                if (i.segmentIndex < curve.PointGroups.Count-1)
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

        public virtual List<SelectableGUID> SelectAll(Curve3D curve)
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
        InterpolationMode InterpolationMode { get; set; } 
    }
    public interface IDistanceSampler : IActiveElement
    {
        string GetLabel();
        EditMode GetEditMode();
        IEnumerable<ISamplerPoint> GetPoints(BezierCurve curve);
        IEnumerable<ISamplerPoint> AllPoints();
        void RecalculateOpenCurveOnlyPoints(BezierCurve curve);
        void Sort(BezierCurve curve);
        int InsertPointAtDistance(float distance,BezierCurve curve);
    }
    public interface IValueSampler<T> : IValueSampler
    {
        T ConstValue { get; set; }
    }
    public interface IValueSampler : IDistanceSampler
    {
        ValueType ValueType { get; set; }
    }
}
