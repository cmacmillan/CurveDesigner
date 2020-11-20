using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    [System.Serializable]
    public class ExtrudePoint : SamplerPoint<BezierCurve, ExtrudePoint, ExtrudeSampler> //Gotta make sure to handle a null value
    {
        public override void SelectEdit(Curve3D curve, List<ExtrudePoint> selectedPoints)
        {
            base.SelectEdit(curve, selectedPoints);
            bool oldClosedLoop = value.isClosedLoop;
            bool newClosedLoop = EditorGUILayout.Toggle("IsClosedLoop",oldClosedLoop);
            value.isClosedLoop = newClosedLoop;
            if (newClosedLoop != oldClosedLoop)
                curve.UICurve.Initialize();
        }
        public override BezierCurve CloneValue(BezierCurve value,bool createNewGuids)
        {
            return new BezierCurve(value,createNewGuids);
        }
    }
    [System.Serializable]
    public class ExtrudeSampler : DistanceSampler<BezierCurve, ExtrudePoint,ExtrudeSampler>
    {
        public ExtrudeSampler(string label, Curve3DEditMode editMode) : base(label,editMode) { }

        public ExtrudeSampler(ExtrudeSampler objToClone, bool createNewGuids) : base(objToClone,createNewGuids) { }
        public override void ConstantField(Rect rect)
        {
            throw new InvalidOperationException();
        }
        public override bool Delete(List<SelectableGUID> guids, Curve3D curve)
        {
            //first we try to delete the curve points
            bool didDelete = base.Delete(guids, curve);
            //now we loop over all the remaining points and try to delete selected points also
            foreach (var extrudeCurve in points)
            {
                extrudeCurve.value.DontDeleteAllTheGuids(guids);
                didDelete |= extrudeCurve.value.DeleteGuids(guids, curve);
            }
            return didDelete;
        }
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
                newPoint = new BezierCurve(newPoint,true);
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
        public Vector3 SampleAt(float primaryCurveDistance,float secondaryCurveDistance, BezierCurve primaryCurve,out Vector3 reference,out Vector3 tangent)
        {
            //This needs to interpolate references smoothly
            Vector3 SamplePosition(ExtrudePoint point, out Vector3 myRef,out Vector3 myTan)
            {
                var samp = point.value.GetPointAtDistance(secondaryCurveDistance * point.value.GetLength());
                myRef = samp.reference;
                myTan = samp.tangent;
                return samp.position;
            }
            Vector3 InterpolateSamples(ExtrudePoint lowerCurve,ExtrudePoint upperCurve,float lowerDistance,float upperDistance,out Vector3 interpolatedReference,out Vector3 interpolatedTangent)
            {
                float distanceBetweenSegments = upperDistance- lowerDistance;
                Vector3 lowerPosition = SamplePosition(lowerCurve, out Vector3 lowerRef,out Vector3 lowerTangent);
                if (lowerCurve.InterpolationMode == KeyframeInterpolationMode.Flat)
                {
                    interpolatedReference = lowerRef;
                    interpolatedTangent = lowerTangent;
                    return lowerPosition;
                }
                Vector3 upperPosition = SamplePosition(upperCurve, out Vector3 upperRef, out Vector3 upperTangent);
                float lerpVal = (primaryCurveDistance - lowerDistance) / distanceBetweenSegments;
                interpolatedReference = Vector3.Lerp(lowerRef, upperRef, lerpVal);
                interpolatedTangent = Vector3.Lerp(lowerTangent, upperTangent, lerpVal);
                return Vector3.Lerp(lowerPosition, upperPosition, lerpVal);
            }
            var availableCurves = GetPoints(primaryCurve);
            if (availableCurves.Count == 0)
            {
                var point = primaryCurve.GetPointAtDistance(primaryCurveDistance);
                reference = point.reference;
                tangent = point.tangent;
                return point.position;
            }
            float previousDistance = availableCurves[0].GetDistance(primaryCurve);
            if (availableCurves.Count==1 || (previousDistance > primaryCurveDistance && !primaryCurve.isClosedLoop))
                return SamplePosition(availableCurves[0], out reference,out tangent);
            if (previousDistance > primaryCurveDistance && primaryCurve.isClosedLoop)

            {
                var lower = availableCurves[availableCurves.Count - 1];
                var upper = availableCurves[0];
                var lowerDistance = lower.GetDistance(primaryCurve)-primaryCurve.GetLength();
                var upperDistance = upper.GetDistance(primaryCurve);
                return InterpolateSamples(lower,upper,lowerDistance,upperDistance,out reference,out tangent);
            }
            ExtrudePoint previousCurve = availableCurves[0];
            for (int i = 1; i < availableCurves.Count; i++)
            {
                var currCurve = availableCurves[i];
                float currentDistance = currCurve.GetDistance(primaryCurve);
                if (currentDistance > primaryCurveDistance)
                    return InterpolateSamples(previousCurve,currCurve,previousDistance,currentDistance,out reference,out tangent);
                previousDistance = currentDistance;
                previousCurve = currCurve;
            }
            if (!primaryCurve.isClosedLoop)
                return SamplePosition(availableCurves[availableCurves.Count - 1], out reference, out tangent);
            else
            {
                var lower = availableCurves[availableCurves.Count - 1];
                var upper = availableCurves[0];
                var lowerDistance = lower.GetDistance(primaryCurve);
                var upperDistance = upper.GetDistance(primaryCurve)+primaryCurve.GetLength();
                return InterpolateSamples(lower,upper,lowerDistance,upperDistance,out reference,out tangent);
            }
        }
    }
}
