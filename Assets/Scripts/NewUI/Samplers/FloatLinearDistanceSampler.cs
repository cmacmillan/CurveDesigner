using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    [System.Serializable]
    public class CurveTrackingDistance
    {
        public virtual void SetDistance(float distance,BezierCurve curve, bool shouldSort = true)
        {
            var point = curve.GetPointAtDistance(distance);
            _segmentIndex = point.segmentIndex;
            _timeAlongSegment = point.time;
        }
        public float GetDistance(BezierCurve curve)
        {
            return curve.GetDistanceAtSegmentIndexAndTime(_segmentIndex, _timeAlongSegment);
        }
        [SerializeField]
        protected float _timeAlongSegment=0;
        [SerializeField]
        protected int _segmentIndex=0;
    }
    [System.Serializable]
    public class FloatDistanceValue : CurveTrackingDistance, ILinePoint
    {
        public float value;
        [NonSerialized]
        public FloatLinearDistanceSampler _owner;
        public FloatDistanceValue(float value, float distance, FloatLinearDistanceSampler owner, BezierCurve curve)
        {
            this.value = value;
            this._owner = owner;
            this.SetDistance(distance,curve);
        }
        public FloatDistanceValue(FloatDistanceValue objToClone,FloatLinearDistanceSampler newOwner,BezierCurve curve)
        {
            this.value = objToClone.value;
            _owner = newOwner;
            this._timeAlongSegment = objToClone._timeAlongSegment;
            this._segmentIndex = objToClone._segmentIndex;
        }
        public override void SetDistance(float distance, BezierCurve curve,bool shouldSort=true)
        {
            base.SetDistance(distance, curve);
            if (shouldSort)
                _owner.SortPoints(curve);
        }
    }
    [System.Serializable]
    public class FloatLinearDistanceSampler : IDistanceSampler<float>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<FloatDistanceValue> _points = new List<FloatDistanceValue>();
        public FloatLinearDistanceSampler() {
        }
        public FloatLinearDistanceSampler(FloatLinearDistanceSampler objToClone, BezierCurve curve)
        {
            foreach (var i in objToClone._points)
                _points.Add(new FloatDistanceValue(i,this,curve));
        }
        //////////////
        List<float> backingCurveModificationDistances;
        public void StartInsertToBackingCurve(BezierCurve backingCurve)
        {
            backingCurveModificationDistances = new List<float>();
            foreach (var i in _points)
                backingCurveModificationDistances.Add(i.GetDistance(backingCurve));
        }
        public void FinishInsertToBackingCurve(BezierCurve backingCurve)
        {
            for (int i = 0; i < _points.Count; i++)
                _points[i].SetDistance(backingCurveModificationDistances[i],backingCurve,false);
            backingCurveModificationDistances = null;
        }
        //////////////
        public float GetAreaUnderCurveUpToDistance(float distance, bool isClosedLoop, float curveLength, BezierCurve curve)
        {
            if (isClosedLoop)
                throw new NotImplementedException();
            if (_points.Count == 0)
                return 0;
            var previousPoint = _points[0];
            var previousDistance = previousPoint.GetDistance(curve);
            float areaUnderCurve = 0;
            float AreaBeneathTwoPoints(float x1,float y1, float x2, float y2)
            {
                return ((y2 - y1)/2)*(x2-x1);
            }
            for (int i = 1; i < _points.Count; i++)
            {
                var currPoint = _points[i];
                float currDistance = currPoint.GetDistance(curve);
                if (currDistance < distance) 
                {
                    areaUnderCurve += AreaBeneathTwoPoints(previousDistance, previousPoint.value, currDistance, currPoint.value);
                } else //then this is the segment baybeee
                {
                    float segmentLength = currDistance - previousDistance;
                    float currentY = Mathf.Lerp(previousPoint.value, currPoint.value, (distance - previousDistance) / segmentLength);
                    areaUnderCurve += AreaBeneathTwoPoints(previousDistance,previousPoint.value,distance,currentY);
                    return areaUnderCurve;
                }
                previousPoint = currPoint;
                previousDistance = currDistance;
            }
            return areaUnderCurve;
        }
        public float GetValueAtDistance(float distance,bool isClosedLoop,float curveLength,BezierCurve curve)
        {
            if (_points.Count == 0)
                return 0;
            var firstPoint = _points[0];
            var lastPoint = _points[_points.Count - 1];
            var lastDistance = curveLength - lastPoint.GetDistance(curve);
            float endSegmentDistance = firstPoint.GetDistance(curve)+ lastDistance;
            if (_points[0].GetDistance(curve)>= distance)
                if (isClosedLoop)
                {
                    float lerpVal = (lastDistance+distance)/endSegmentDistance;
                    return Mathf.Lerp(lastPoint.value,firstPoint.value,lerpVal);
                }
                else
                    return _points[0].value;
            var previous = _points[0];
            for (int i = 1; i < _points.Count; i++)
            {
                var current = _points[i];
                if (current.GetDistance(curve)>= distance)
                    return Mathf.Lerp(previous.value,current.value,(distance-previous.GetDistance(curve))/(current.GetDistance(curve)-previous.GetDistance(curve)));
                previous = current;
            }
            if (isClosedLoop)
            {
                float lerpVal = (distance-lastPoint.GetDistance(curve)) / endSegmentDistance;
                return Mathf.Lerp(lastPoint.value,firstPoint.value,lerpVal);
            }
            else
                return _points[_points.Count - 1].value;
        }
        public int InsertPointAtDistance(float distance,bool isClosedLoop,float curveLength,BezierCurve curve)
        {
            var value = GetValueAtDistance(distance, isClosedLoop, curveLength,curve);
            var newPoint = new FloatDistanceValue(value, distance, this,curve);
            _points.Add(newPoint);
            SortPoints(curve);
            return _points.IndexOf(newPoint);
        }
        public void SortPoints(BezierCurve curve)
        {
            _points = _points.OrderBy((a) => a.GetDistance(curve)).ToList();
        }
        public List<FloatDistanceValue> GetPoints(Curve3D curve)
        {
            return GetPointsBelowDistance(curve.positionCurve.GetLength(),curve.positionCurve);
        }
        private List<FloatDistanceValue> GetPointsBelowDistance(float distance,BezierCurve curve)
        {
            List<FloatDistanceValue> retr = new List<FloatDistanceValue>();
            foreach (var i in _points)
                if (i.GetDistance(curve) <= distance)
                    retr.Add(i);
            return retr;
        }

        public void OnBeforeSerialize()
        {
            //Do nothing
        }

        public void OnAfterDeserialize()
        {
            foreach (var i in _points)
                i._owner = this;
        }
    }
}
