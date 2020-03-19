using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    [System.Serializable]
    public class FloatDistanceValue : ILinePoint
    {
        public float value;
        [NonSerialized]
        public FloatLinearDistanceSampler _owner;
        [SerializeField]
        private float _distance;
        public FloatDistanceValue(float value, float distance, FloatLinearDistanceSampler owner)
        {
            this.value = value;
            this._owner = owner;
            this._distance = distance;
        }
        public FloatDistanceValue(FloatDistanceValue objToClone,FloatLinearDistanceSampler newOwner)
        {
            this.value = objToClone.value;
            this._distance = objToClone._distance;
            _owner = newOwner;
        } 
        public float Distance {
            get { return _distance; }
            set
            {
                _distance = value;
                _owner.SortPoints();
            }
        }

        public float DistanceAlongCurve { get { return _distance; } set { _distance = value; } }
    }
    [System.Serializable]
    public class FloatLinearDistanceSampler : IDistanceSampler<float>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<FloatDistanceValue> _points = new List<FloatDistanceValue>();
        public FloatLinearDistanceSampler() {
        }
        public FloatLinearDistanceSampler(FloatLinearDistanceSampler objToClone)
        {
            foreach (var i in objToClone._points)
                _points.Add(new FloatDistanceValue(i,this));
        }
        public float GetValueAtDistance(float distance,bool isClosedLoop,float curveLength,float? defaultValue=null)
        {
            if (_points.Count == 0)
                return (defaultValue.HasValue?defaultValue.Value:default);
            var firstPoint = _points[0];
            var lastPoint = _points[_points.Count - 1];
            var lastDistance = curveLength - lastPoint.Distance;
            float endSegmentDistance = firstPoint.Distance + lastDistance;
            if (_points[0].Distance >= distance)
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
                if (current.Distance >= distance)
                    return Mathf.Lerp(previous.value,current.value,(distance-previous.Distance)/(current.Distance-previous.Distance));
                previous = current;
            }
            if (isClosedLoop)
            {
                float lerpVal = (distance-lastPoint.Distance) / endSegmentDistance;
                return Mathf.Lerp(lastPoint.value,firstPoint.value,lerpVal);
            }
            else
                return _points[_points.Count - 1].value;
        }
        public void InsertPointAtDistance(float distance,bool isClosedLoop,float curveLength,float? defaultValue=null)
        {
            var value = GetValueAtDistance(distance, isClosedLoop, curveLength,defaultValue);
            _points.Add(new FloatDistanceValue(value, distance, this));
            SortPoints();
        }
        public void SortPoints()
        {
            _points = _points.OrderBy((a) => a.Distance).ToList();
        }
        public List<FloatDistanceValue> GetPoints(Curve3D curve)
        {
            return GetPointsBelowDistance(curve.positionCurve.GetLength());
        }
        private List<FloatDistanceValue> GetPointsBelowDistance(float distance)
        {
            List<FloatDistanceValue> retr = new List<FloatDistanceValue>();
            foreach (var i in _points)
                if (i.Distance <= distance)
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
