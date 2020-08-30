using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    [System.Serializable]
    public class DoubleBezierPoint : SamplerPoint<BezierCurve, DoubleBezierPoint, DoubleBezierSampler> //Gotta make sure to handle a null value
    {
        public override BezierCurve CloneValue(BezierCurve value)
        {
            return new BezierCurve(value);
        }
    }
    [System.Serializable]
    public class DoubleBezierSampler : DistanceSampler<BezierCurve, DoubleBezierPoint,DoubleBezierSampler>
    {
        public DoubleBezierSampler(string label, EditMode editMode) : base(label,editMode) { }

        public DoubleBezierSampler(DoubleBezierSampler objToClone) : base(objToClone) { }
        public override List<SelectableGUID> SelectAll(Curve3D curve)
        {
            List<SelectableGUID> retr = new List<SelectableGUID>();
            var points = GetPoints(curve.positionCurve);
            foreach (var i in points)
            {
                retr.Add(i.GUID);
                foreach (var j in i.value.PointGroups)
                    retr.Add(j.GUID);
            }
            return retr;
        }

        protected override BezierCurve GetInterpolatedValueAtDistance(float distance, BezierCurve curve)
        {
            BezierCurve newPoint=null;
            var openPoints = GetPoints(curve);
            if (openPoints.Count > 0)
            {
                float len = curve.GetLength();
                newPoint = openPoints.OrderBy(a => curve.WrappedDistanceBetween(distance, a.GetDistance(curve))).First().value;
                newPoint = new BezierCurve(newPoint);
            }
            else
            {
                newPoint = new BezierCurve();
                newPoint.owner = curve.owner;
                newPoint.Initialize();
            }
            newPoint.dimensionLockMode = DimensionLockMode.z;
            newPoint.Recalculate();
            return newPoint;
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
}
