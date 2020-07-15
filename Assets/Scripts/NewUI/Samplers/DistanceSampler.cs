using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    [System.Serializable]
    public class SamplerPoint<T,S> where S : SamplerPoint<T,S>,new()
    {
        public T value;
        [NonSerialized]
        public DistanceSampler<T,S> owner;
        public int segmentIndex;
        public float time;
        public float GetDistance(BezierCurve curve)
        {
            return curve.GetDistanceAtSegmentIndexAndTime(segmentIndex,time);
        }
        public void SetDistance(float distance,BezierCurve curve, bool shouldSort = true)
        {
            var point = curve.GetPointAtDistance(distance);
            segmentIndex = point.segmentIndex;
            time = point.time;
            if (shouldSort)
                owner.Sort(curve);
        }
    }

    //Float Sampler
    [System.Serializable]
    public class FloatSamplerPoint : SamplerPoint<float,FloatSamplerPoint> { }
    [System.Serializable]
    public class FloatDistanceSampler : ValueDistanceSampler<float, FloatSamplerPoint>
    {
        public override float GetDefaultVal()
        {
            return 0;
        }

        public override float Lerp(float val1, float val2, float lerp)
        {
            return Mathf.Lerp(val1,val2,lerp);
        }
    }

    //Color Sampler
    [System.Serializable]
    public class ColorSamplerPoint : SamplerPoint<Color, ColorSamplerPoint> { }
    [System.Serializable]
    public class ColorDistanceSampler : ValueDistanceSampler<Color, ColorSamplerPoint>
    {
        public override Color GetDefaultVal()
        {
            return Color.white;
        }

        public override Color Lerp(Color val1, Color val2, float lerp)
        {
            return Color.Lerp(val1,val2,lerp);
        }
    }

    public abstract class ValueDistanceSampler<T,S> : DistanceSampler<T, S> where S : SamplerPoint<T,S>, new()
    {
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

    public class DoubleBezierPoint : SamplerPoint<BezierCurve,DoubleBezierPoint> { }//Gotta make sure to handle a null value
    [System.Serializable]
    public class NewDoubleBezierSampler : DistanceSampler<BezierCurve, DoubleBezierPoint>
    {
        protected override BezierCurve GetInterpolatedValueAtDistance(float distance, BezierCurve curve)
        {
            BezierCurve curveToCopy=null;
            var openPoints = GetPoints(curve);
            if (openPoints.Count > 0)
            {
                float len = curve.GetLength();
                curveToCopy = openPoints.OrderBy(a => curve.WrappedDistanceBetween(distance, a.GetDistance(curve))).First().value;
            }
            return curveToCopy;
        }

        ///Secondary curve distance is a value between 0 and 1
        public Vector3 SampleAt(float primaryCurveDistance,float secondaryCurveDistance, BezierCurve primaryCurve,out Vector3 reference)
        {
            //This needs to interpolate references smoothly
            Vector3 SamplePosition(DoubleBezierPoint point, out Vector3 myRef)
            {
                var samp = point.value.GetPointAtDistance(secondaryCurveDistance * point.value.GetLength());
                myRef = samp.reference;
                return samp.position;
            }
            Vector3 InterpolateSamples(DoubleBezierPoint lowerCurve,DoubleBezierPoint upperCurve,float lowerDistance,float upperDistance,out Vector3 interpolatedReference)
            {
                float distanceBetweenSegments = upperDistance- lowerDistance;
                float lerpVal = (primaryCurveDistance - lowerDistance) / distanceBetweenSegments;
                Vector3 lowerPosition = SamplePosition(lowerCurve, out Vector3 lowerRef);
                Vector3 upperPosition = SamplePosition(upperCurve, out Vector3 upperRef);
                interpolatedReference = Vector3.Lerp(lowerRef, upperRef, lerpVal);
                return Vector3.Lerp(lowerPosition, upperPosition, lerpVal);
            }
            var availableCurves = GetPoints(primaryCurve);
            if (availableCurves.Count == 0)
            {
                var point = primaryCurve.GetPointAtDistance(primaryCurveDistance);
                reference = point.reference;
                return point.position;
            }
            float previousDistance = availableCurves[0].GetDistance(primaryCurve);
            if (availableCurves.Count==1 || (previousDistance > primaryCurveDistance && !primaryCurve.isClosedLoop))
                return SamplePosition(availableCurves[0], out reference);
            if (previousDistance > primaryCurveDistance && primaryCurve.isClosedLoop)

            {
                var lower = availableCurves[availableCurves.Count - 1];
                var upper = availableCurves[0];
                var lowerDistance = lower.GetDistance(primaryCurve)-primaryCurve.GetLength();
                var upperDistance = upper.GetDistance(primaryCurve);
                return InterpolateSamples(lower,upper,lowerDistance,upperDistance,out reference);
            }
            DoubleBezierPoint previousCurve = availableCurves[0];
            for (int i = 1; i < availableCurves.Count; i++)
            {
                var currCurve = availableCurves[i];
                float currentDistance = currCurve.GetDistance(primaryCurve);
                if (currentDistance > primaryCurveDistance)
                    return InterpolateSamples(previousCurve,currCurve,previousDistance,currentDistance,out reference);
                previousDistance = currentDistance;
                previousCurve = currCurve;
            }
            if (!primaryCurve.isClosedLoop)
                return SamplePosition(availableCurves[availableCurves.Count - 1], out reference);
            else
            {
                var lower = availableCurves[availableCurves.Count - 1];
                var upper = availableCurves[0];
                var lowerDistance = lower.GetDistance(primaryCurve);
                var upperDistance = upper.GetDistance(primaryCurve)+primaryCurve.GetLength();
                return InterpolateSamples(lower,upper,lowerDistance,upperDistance,out reference);
            }
        }
    }

    [System.Serializable]
    public abstract class DistanceSampler<T,S> : ISerializationCallbackReceiver, IDistanceSampler where S : SamplerPoint<T,S>,new()
    {
        public List<S> points = new List<S>();

        [NonSerialized]
        private List<S> points_openCurveOnly = null;

        protected abstract T GetInterpolatedValueAtDistance(float distance, BezierCurve curve);

        public int InsertPointAtDistance(float distance, BezierCurve curve) {
            T interpolatedValue = GetInterpolatedValueAtDistance(distance, curve);
            var newPoint = new S();
            newPoint.value = interpolatedValue;
            newPoint.owner = this;
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
                i.owner = this;
        }
    }
    public interface IDistanceSampler
    {
        void RecalculateOpenCurveOnlyPoints(BezierCurve curve);
        void Sort(BezierCurve curve);
        int InsertPointAtDistance(float distance,BezierCurve curve);
    }
}
