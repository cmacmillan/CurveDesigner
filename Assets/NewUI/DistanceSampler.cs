using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public interface IDistanceSampler<T> where T : struct
    {
        T GetValueAtDistance(float distance,T? defaultValue=null);
    }
    [System.Serializable]
    public class DistanceValue<T> where T : struct
    {
        public T value;
        [SerializeField]
        private LinearValueDistanceSampler<T> _owner;
        [SerializeField]
        private float _distance;
        public DistanceValue(T value, float distance, LinearValueDistanceSampler<T> owner)
        {
            this.value = value;
            this._owner = owner;
            this._distance = distance;
        }
        public DistanceValue(DistanceValue<T> objToClone, LinearValueDistanceSampler<T> newOwner)
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
    [Serializable]
    public class LinearValueDistanceSampler<T> : IDistanceSampler<T> where T : struct
    {
        [SerializeField]
        private Func<T, T, float, T> _lerpFunction;
        [SerializeField]
        private List<DistanceValue<T>> _points = new List<DistanceValue<T>>();
        public LinearValueDistanceSampler(Func<T,T,float,T> lerpFunction)
        {
            this._lerpFunction = lerpFunction;
        }
        public LinearValueDistanceSampler(LinearValueDistanceSampler<T> objToClone)
        {
            this._lerpFunction = objToClone._lerpFunction;
            foreach (var i in objToClone._points)
                _points.Add(new DistanceValue<T>(i,this));
        }
        public T GetValueAtDistance(float distance,T? defaultValue=null)
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
                    return _lerpFunction(previous.value,current.value,(distance-previous.Distance)/(current.Distance-previous.Distance));
                previous = current;
            }
            return _points[_points.Count - 1].value;
        }
        public void InsertPointAtDistance(float distance,T? defaultValue=null)
        {
            var value = GetValueAtDistance(distance, defaultValue);
            _points.Add(new DistanceValue<T>(value, distance, this));
            SortPoints();
        }
        public void SortPoints()
        {
            _points.OrderBy((a) => a.Distance);
        }
        public List<DistanceValue<T>> GetPointsBelowDistance(float distance)
        {
            List<DistanceValue<T>> retr = new List<DistanceValue<T>>();
            foreach (var i in _points)
                if (i.Distance <= distance)
                    retr.Add(i);
            return retr;
        }
    }
    [System.Serializable]
    public class FloatLinearValueDistanceSampler : LinearValueDistanceSampler<float>
    {
        public FloatLinearValueDistanceSampler() : base(Mathf.Lerp) { }
    }
}
