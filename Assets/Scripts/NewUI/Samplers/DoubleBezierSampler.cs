using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.NewUI
{
    [System.Serializable]
    public class BezierCurveDistanceValue : CurveTrackingValue
    {
        [NonSerialized]
        public DoubleBezierSampler _owner;
        public BezierCurve secondaryCurve;
        public BezierCurveDistanceValue(DoubleBezierSampler owner,float distance,BezierCurve curve,BezierCurve curveToCopy) : base(distance, curve)
        {
            _owner = owner;
            _owner.SortPoints(curve);
            if (curveToCopy == null)
            {
                secondaryCurve = new BezierCurve();
                secondaryCurve.Initialize();
            }
            else
            {
                secondaryCurve = new BezierCurve(curveToCopy);
            }
            secondaryCurve.owner = curve.owner;
            secondaryCurve.dimensionLockMode = DimensionLockMode.z;
            secondaryCurve.Recalculate();
        }
        public BezierCurveDistanceValue(BezierCurveDistanceValue objToClone,DoubleBezierSampler newOwner) : base (objToClone)
        {
            _owner = newOwner;
            secondaryCurve = new BezierCurve(objToClone.secondaryCurve);
        }
        public override void SetDistance(float distance, BezierCurve curve, bool shouldSort = true)
        {
            base.SetDistance(distance, curve, shouldSort);
            if (shouldSort)
                _owner.SortPoints(curve);
        }
    }
    [System.Serializable]
    public class DoubleBezierSampler : ISerializationCallbackReceiver
    {
        private List<BezierCurveDistanceValue> openCurveSecondaryCurves;
        public List<BezierCurveDistanceValue> secondaryCurves = new List<BezierCurveDistanceValue>();
        public DoubleBezierSampler()
        {

        }
        public DoubleBezierSampler(DoubleBezierSampler objToClone)
        {
            foreach (var i in objToClone.secondaryCurves)
                secondaryCurves.Add(new BezierCurveDistanceValue(i,this));
            openCurveSecondaryCurves = new List<BezierCurveDistanceValue>();
            foreach (var i in objToClone.openCurveSecondaryCurves)
                openCurveSecondaryCurves.Add(new BezierCurveDistanceValue(i,this));
        }
        public List<BezierCurveDistanceValue> GetPoints(Curve3D curve)
        {
            return GetPointsByCurveOpenClosedStatus(curve.positionCurve);
        }
        public void CacheOpenCurvePoints(BezierCurve curve)
        {
            openCurveSecondaryCurves = new List<BezierCurveDistanceValue>();
            foreach (var i in secondaryCurves)
                if (i.SegmentIndex < curve.NumSegments)
                    openCurveSecondaryCurves.Add(i);
        }
        private List<BezierCurveDistanceValue> GetPointsByCurveOpenClosedStatus(BezierCurve curve, bool recalculate = true)//recalculate=false is much faster, but requires having cached earlier
        {
            if (recalculate)
                CacheOpenCurvePoints(curve);
            if (curve.isClosedLoop)
                return secondaryCurves;
            else
                return openCurveSecondaryCurves;
        }
        public void SortPoints(BezierCurve curve)
        {
            secondaryCurves = secondaryCurves.OrderBy((a => a.TimeAlongSegment)).OrderBy(a => a.SegmentIndex).ToList();
            CacheOpenCurvePoints(curve);
        }
        private float WrappedDistanceBetween(float distance1, float distance2,float length, bool isClosed)
        {
            float simpleDistance = Mathf.Abs(distance1 - distance2);
            if (isClosed)
            {
                float wrappedUpper = Mathf.Abs((distance2-length)-distance1);
                float wrappedLower = Mathf.Abs((distance1-length)-distance2);
                return Mathf.Min(simpleDistance,wrappedLower,wrappedUpper);
            } else
            {
                return simpleDistance;
            }
        }
        public int InsertPointAtDistance(float distance,bool isClosedLoop,float curveLength,BezierCurve curve)
        {
            BezierCurve curveToCopy=null;
            var openPoints = GetPointsByCurveOpenClosedStatus(curve);
            if (openPoints.Count > 0)
            {
                float len = curve.GetLength();
                curveToCopy = openPoints.OrderBy(a => WrappedDistanceBetween(distance, a.GetDistance(curve),len,isClosedLoop)).First().secondaryCurve;
            }
            var newPoint = new BezierCurveDistanceValue(this, distance,curve,curveToCopy);
            secondaryCurves.Add(newPoint);
            SortPoints(curve);
            return secondaryCurves.IndexOf(newPoint);
        }
        ///Secondary curve distance is a value between 0 and 1
        public Vector3 SampleAt(float primaryCurveDistance,float secondaryCurveDistance, BezierCurve primaryCurve,out Vector3 reference)
        {
            //This needs to interpolate references smoothly
            Vector3 SamplePosition(BezierCurveDistanceValue value, out Vector3 myRef)
            {
                var samp = value.secondaryCurve.GetPointAtDistance(secondaryCurveDistance * value.secondaryCurve.GetLength());
                myRef = samp.reference;
                return samp.position;
            }
            Vector3 InterpolateSamples(BezierCurveDistanceValue lowerCurve,BezierCurveDistanceValue upperCurve,float lowerDistance,float upperDistance,out Vector3 interpolatedReference)
            {
                float distanceBetweenSegments = upperDistance- lowerDistance;
                float lerpVal = (primaryCurveDistance - lowerDistance) / distanceBetweenSegments;
                Vector3 lowerPosition = SamplePosition(lowerCurve, out Vector3 lowerRef);
                Vector3 upperPosition = SamplePosition(upperCurve, out Vector3 upperRef);
                interpolatedReference = Vector3.Lerp(lowerRef, upperRef, lerpVal);
                return Vector3.Lerp(lowerPosition, upperPosition, lerpVal);
            }
            var availableCurves = GetPointsByCurveOpenClosedStatus(primaryCurve,false);
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
            BezierCurveDistanceValue previousCurve = availableCurves[0];
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

        public void OnBeforeSerialize()
        {
            //Do nothing
        }

        public void OnAfterDeserialize()
        {
            foreach (var i in secondaryCurves)
                i._owner = this;
        }
    }
}
