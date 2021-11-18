using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    /// <summary>
    /// <para>A sampler represents a series of points of type T, each of which can be positioned at a particular point along a Curve3D</para>
    /// <para>You can sample at any distance along the curve and will recieve a value calculated by interpolating between the two adjacent points</para>
    /// </summary>
    /// <typeparam name="T">The type of points in the Sampler</typeparam>
    public abstract class Sampler<T> : ISampler<T>,ISerializationCallbackReceiver
    {
        public List<SamplerPoint<T>> points = new List<SamplerPoint<T>>();

        [NonSerialized]
        //this value is essentially just a cache of the points excluding all points that are within the final segment of the curve when it is a closed loop
        //this is because those values essentially need to temporarily disappear when IsClosedLoop gets disabled
        private List<SamplerPoint<T>> points_openCurveOnly = null;

        public string fieldDisplayName="";

        [SerializeField]
        private string label;
        [SerializeField]
        private Curve3DEditMode editMode;
        public Sampler(string label, Curve3DEditMode editMode) {
            this.label = label;
            this.editMode = editMode;
        }
        public Sampler(Sampler<T> objToClone,bool createNewGuids,Curve3D curve)
        {
            this.label = objToClone.label;
            this.editMode = objToClone.editMode;

            points_openCurveOnly = new List<SamplerPoint<T>>();

            foreach (var i in objToClone.points)
            {
                var newPoint = new SamplerPoint<T>(i, this, createNewGuids, curve);
                points.Add(newPoint);
                if (objToClone.points_openCurveOnly.Contains(i))
                    points_openCurveOnly.Add(newPoint);
            }
        }
        protected abstract T GetInterpolatedValueAtDistance(float distance, BezierCurve curve);
        public virtual T CloneValue(T val, bool shouldCreateGuids)
        {
            return val;
        }
        public void CacheDistances(BezierCurve curve)
        {
            foreach (var i in GetPoints(curve))
            {
                i.cachedDistance = i.GetDistance(curve);
            }
        }
#if UNITY_EDITOR
        public virtual void SelectEdit(Curve3D curve, List<SamplerPoint<T>> selectedPoints,SamplerPoint<T> mainPoint)
        {
            float originalDistance = mainPoint.GetDistance(curve.positionCurve);
            float distanceOffset = EditorGUILayout.FloatField("Distance", originalDistance) - originalDistance;
            KeyframeInterpolationMode newInterpolation = (KeyframeInterpolationMode)EditorGUILayout.EnumPopup("Interpolation",mainPoint.InterpolationMode);
            if (newInterpolation != mainPoint.InterpolationMode)
                foreach (var i in selectedPoints)
                    i.InterpolationMode = newInterpolation;
            if (distanceOffset == 0)
                return;
            EditorGUIUtility.SetWantsMouseJumping(1);
            PointOnCurveClickCommand.ClampOffset(distanceOffset, curve, selectedPoints);
        }
#endif
        public string GetLabel()
        {
            return label;
        }

        public Curve3DEditMode GetEditMode()
        {
            return editMode;
        }

        public IEnumerable<ISamplerPoint> AllPoints()
        {
            return points;
        }

        IEnumerable<ISamplerPoint> ISampler.GetPoints(BezierCurve curve)
        {
            return points;
        }

        public int InsertPointAtDistance(float distance, BezierCurve curve) {
            T interpolatedValue = GetInterpolatedValueAtDistance(distance, curve);
            var newPoint = new SamplerPoint<T>(this,curve.owner);
            newPoint.value = interpolatedValue;
            var valuePoint = newPoint as ISamplerPoint;
            if (valuePoint != null && TryGetPointBelowDistance(distance, curve, out SamplerPoint<T> point))
                valuePoint.InterpolationMode = point.InterpolationMode;
            points.Add(newPoint);
            newPoint.SetDistance(distance,curve);
            return points.IndexOf(newPoint);
        }

        private bool TryGetPointBelowDistance(float distance, BezierCurve curve,out SamplerPoint<T> point)
        {
            point = null;
            var points = GetPoints(curve);
            if (points.Count == 0)
                return false;
            if (distance < points[0].GetDistance(curve)){
                if (curve.isClosedLoop)
                    point = points[points.Count - 1];
                else
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
        public List<SamplerPoint<T>> GetPoints(BezierCurve curve)
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
            points_openCurveOnly = new List<SamplerPoint<T>>();
            foreach (var i in points)
                if (i.segmentIndex < curve.PointGroups.Count-1)
                    points_openCurveOnly.Add(i);
        }

        public void Sort(BezierCurve curve)
        {
            points = points.OrderBy((a) => a.time).OrderBy(a=>a.segmentIndex).ToList();
            RecalculateOpenCurveOnlyPoints(curve);
        }

        public void OnBeforeSerialize() {}

        public void OnAfterDeserialize()
        {
            foreach (var i in points)
                i.owner = this;
        }

        public virtual ISelectable GetSelectable(int index, Curve3D curve)
        {
            return GetPoints(curve.positionCurve)[index];
        }

        public virtual int NumSelectables(Curve3D curve)
        {
            return GetPoints(curve.positionCurve).Count;
        }

        public virtual bool Delete(List<SelectableGUID> guids, Curve3D curve)
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

        public string GetPointName()
        {
            return label.ToLower();
        }
    }
}
