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
        public int SegmentIndex { get { return _segmentIndex; } }
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
        public float GetDistanceByAreaUnderInverseCurve(float targetAreaUnderCurve, bool isClosedLoop, float curveLength, BezierCurve curve,float baseVal)
        {
            var pointsInsideCurve = GetPointsByCurveOpenClosedStatus(curve);
            if (pointsInsideCurve.Count == 0)
                return targetAreaUnderCurve / baseVal;
            var previousPoint = pointsInsideCurve[0];
            var previousDistance = previousPoint.GetDistance(curve);
            float areaUnderCurve = 0;
            var startingHeight = GetVal(previousPoint);
            float firstSegmentArea;
            float valueAtStartOfCurve = -1;//only used when a closed loop
            if (isClosedLoop)
            {
                var pointBefore = pointsInsideCurve[pointsInsideCurve.Count - 1];
                float distanceFromPointBeforeToEndOfCurve = curveLength - pointBefore.GetDistance(curve);
                valueAtStartOfCurve = Mathf.Lerp(GetVal(pointBefore),startingHeight,distanceFromPointBeforeToEndOfCurve/(distanceFromPointBeforeToEndOfCurve+previousDistance));
                firstSegmentArea = AreaBeneathTwoPoints(0, valueAtStartOfCurve, previousDistance, startingHeight);
                if (targetAreaUnderCurve < firstSegmentArea)
                    return FindDistanceOfArea(targetAreaUnderCurve, 0, valueAtStartOfCurve, previousDistance, startingHeight);
            } else
            {
                firstSegmentArea = AreaBeneathTwoPoints(0, startingHeight, previousDistance, startingHeight);
                if (targetAreaUnderCurve < firstSegmentArea)
                    return FindDistanceOfArea(targetAreaUnderCurve, -1, startingHeight, -1, startingHeight);
            }
            areaUnderCurve += firstSegmentArea;
            float GetVal(FloatDistanceValue val)
            {
                return 1.0f / (val.value+baseVal);
            }
            float AreaBeneathTwoPoints(float x1,float y1, float x2, float y2)
            {
                return ((y2 + y1)/2)*(x2-x1);
            }
            float square(float val)
            {
                return val * val;
            }
            float FindDistanceOfArea(float area,float x1, float y1, float x2,float y2)
            {
                if (y2==y1)//if flat
                    return area / y1;
                float b = (y2-y1)/(x2-x1);//slope
                float a = area;
                float denom = 2 * b;
                float numer1 = -2*y1;
                float numer2 = Mathf.Sqrt(square(2*y1)+4*b*2*a);
                return (numer1+numer2) / denom;
            }
            for (int i = 1; i < pointsInsideCurve.Count; i++)
            {
                var currPoint = pointsInsideCurve[i];
                float currDistance = currPoint.GetDistance(curve);
                float currSegmentArea = AreaBeneathTwoPoints(previousDistance,GetVal(previousPoint),currDistance,GetVal(currPoint));
                if (areaUnderCurve + currSegmentArea > targetAreaUnderCurve)//then this is the segment
                {
                    return previousDistance + FindDistanceOfArea(targetAreaUnderCurve-areaUnderCurve,previousDistance,GetVal(previousPoint),currDistance,GetVal(currPoint));
                }
                areaUnderCurve += currSegmentArea;
                previousPoint = currPoint;
                previousDistance = currDistance;
            }
            float finalPointVal = GetVal(previousPoint);
            if (isClosedLoop)
                return previousDistance + FindDistanceOfArea(targetAreaUnderCurve - areaUnderCurve, previousDistance, finalPointVal, curveLength, valueAtStartOfCurve);
            else
                return previousDistance + FindDistanceOfArea(targetAreaUnderCurve - areaUnderCurve, -1, finalPointVal, -1, finalPointVal);
        }
        public float GetValueAtDistance(float distance,bool isClosedLoop,float curveLength,BezierCurve curve)
        {
            var pointsInsideCurve = GetPointsByCurveOpenClosedStatus(curve);
            if (pointsInsideCurve.Count == 0)
                return 0;
            var firstPoint = pointsInsideCurve[0];
            var lastPoint = pointsInsideCurve[pointsInsideCurve.Count - 1];
            var lastDistance = curveLength - lastPoint.GetDistance(curve);
            float endSegmentDistance = firstPoint.GetDistance(curve)+ lastDistance;
            if (pointsInsideCurve[0].GetDistance(curve)>= distance)
                if (isClosedLoop)
                {
                    float lerpVal = (lastDistance+distance)/endSegmentDistance;
                    return Mathf.Lerp(lastPoint.value,firstPoint.value,lerpVal);
                }
                else
                    return pointsInsideCurve[0].value;
            var previous = pointsInsideCurve[0];
            for (int i = 1; i < pointsInsideCurve.Count; i++)
            {
                var current = pointsInsideCurve[i];
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
                return pointsInsideCurve[pointsInsideCurve.Count - 1].value;
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
            CacheOpenCurvePoints(curve);
        }
        public List<FloatDistanceValue> GetPoints(Curve3D curve)
        {
            return GetPointsByCurveOpenClosedStatus(curve.positionCurve);
        }
        private List<FloatDistanceValue> openCurvePoints;
        public void CacheOpenCurvePoints(BezierCurve curve)
        {
            openCurvePoints = new List<FloatDistanceValue>();
            foreach (var i in _points)
                if (i.SegmentIndex<curve.NumSegments)
                    openCurvePoints.Add(i);
        }
        private List<FloatDistanceValue> GetPointsByCurveOpenClosedStatus(BezierCurve curve, bool recalculate=true)//recalculate=false is much faster, but requires having cached earlier
        {
            if (recalculate)
                CacheOpenCurvePoints(curve);
            if (curve.isClosedLoop)
                return _points;
            else
                return openCurvePoints;
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
