using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    [System.Serializable]
    public class FloatDistanceValue
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
    }
    [System.Serializable]
    public class FloatLinearDistanceSampler : IDistanceSampler<float>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<FloatDistanceValue> _points = new List<FloatDistanceValue>();
        public FloatLinearDistanceSampler() { }
        public FloatLinearDistanceSampler(FloatLinearDistanceSampler objToClone)
        {
            foreach (var i in objToClone._points)
                _points.Add(new FloatDistanceValue(i,this));
        }
        public float GetValueAtDistance(float distance,float? defaultValue=null)
        {
            if (_points.Count == 0)
                return (defaultValue.HasValue?defaultValue.Value:default);
            if (_points[0].Distance <= distance)
                return _points[0].value;
            var previous = _points[0];
            for (int i = 1; i < _points.Count; i++)
            {
                var current = _points[i];
                if (current.Distance >= distance)
                    return Mathf.Lerp(previous.value,current.value,(distance-previous.Distance)/(current.Distance-previous.Distance));
                previous = current;
            }
            return _points[_points.Count - 1].value;
        }
        public void InsertPointAtDistance(float distance,float? defaultValue=null)
        {
            var value = GetValueAtDistance(distance, defaultValue);
            _points.Add(new FloatDistanceValue(value, distance, this));
            SortPoints();
        }
        public void SortPoints()
        {
            _points.OrderBy((a) => a.Distance);
        }
        public List<FloatDistanceValue> GetPointsBelowDistance(float distance)
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
