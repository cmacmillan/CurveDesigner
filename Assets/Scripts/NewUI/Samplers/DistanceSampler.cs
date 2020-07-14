using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    [System.Serializable]
    public struct SegmentDistance
    {
        public int segmentIndex;
        public float time;
        public SegmentDistance(int segmentIndex, float time)
        {
            this.segmentIndex = segmentIndex;
            this.time = time;
        }
    }

    //Not serializable
    public class SamplerPoint<T>
    {
        public T value;
        public SegmentDistance segmentDistance;
        public DistanceSampler<T> owner;
        public SamplerPoint(T value, SegmentDistance segmentDistance){
            this.value = value;
            this.segmentDistance = segmentDistance;
        }
        public float GetDistance(BezierCurve curve)
        {
            return curve.GetDistanceAtSegmentIndexAndTime(segmentDistance.segmentIndex, segmentDistance.segmentIndex);
        }
        public void SetDistance(float distance,BezierCurve curve, bool shouldSort = true)
        {
            var point = curve.GetPointAtDistance(distance);
            segmentDistance = new SegmentDistance(point.segmentIndex,point.time);
            if (shouldSort)
                owner.Sort();
        }
    }

    //Not serializable
    public class DistanceSampler<T>
    {
        public List<SamplerPoint<T>> points;
        public delegate T Lerp(T from, T to, float time);
        public delegate T GetDefaultVal();
        private Lerp lerp;
        private GetDefaultVal defaultVal;
        public DistanceSampler(List<T> items,List<SegmentDistance> distances, Lerp lerpFunction,GetDefaultVal defaultVal){
            if (items.Count != distances.Count)
                throw new ArgumentException("Items and distances must have same Count.");
            this.defaultVal = defaultVal;
            points = new List<SamplerPoint<T>>();
            for (int i=0;i<items.Count;i++)
                points.Add(new SamplerPoint<T>(items[i],distances[i]));
        }
        public void Sort()
        {
            points = points.OrderBy((a) => a.segmentDistance.time).OrderBy(a=>a.segmentDistance.segmentIndex).ToList();
            //CacheOpenCurvePoints(curve);
        }
        public T GetValueAtDistance(float distance, bool isClosedLoop, float curveLength, BezierCurve curve)
        {
            //var pointsInsideCurve = GetPointsByCurveOpenClosedStatus(curve);
            var pointsInsideCurve = points;
            if (pointsInsideCurve.Count == 0)
                return defaultVal();
            var firstPoint = pointsInsideCurve[0];
            var lastPoint = pointsInsideCurve[pointsInsideCurve.Count - 1];
            var lastDistance = curveLength - lastPoint.GetDistance(curve);
            float endSegmentDistance = firstPoint.GetDistance(curve) + lastDistance;
            if (pointsInsideCurve[0].GetDistance(curve) >= distance)
                if (isClosedLoop)
                {
                    float lerpVal = (lastDistance + distance) / endSegmentDistance;
                    return lerp(lastPoint.value, firstPoint.value, lerpVal);
                }
                else
                    return pointsInsideCurve[0].value;
            var previous = pointsInsideCurve[0];
            for (int i = 1; i < pointsInsideCurve.Count; i++)
            {
                var current = pointsInsideCurve[i];
                if (current.GetDistance(curve) >= distance)
                    return lerp(previous.value, current.value, (distance - previous.GetDistance(curve)) / (current.GetDistance(curve) - previous.GetDistance(curve)));
                previous = current;
            }
            if (isClosedLoop)
            {
                float lerpVal = (distance - lastPoint.GetDistance(curve)) / endSegmentDistance;
                return lerp(lastPoint.value, firstPoint.value, lerpVal);
            }
            else
                return pointsInsideCurve[pointsInsideCurve.Count - 1].value;
        }
    }
}
