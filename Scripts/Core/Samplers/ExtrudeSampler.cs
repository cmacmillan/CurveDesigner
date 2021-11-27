using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace ChaseMacMillan.CurveDesigner
{
    [System.Serializable]
    public class ExtrudeSamplerPoint : SamplerPoint<BezierCurve,ExtrudeSamplerPoint> { }
    [System.Serializable]
    public class ExtrudeSampler : Sampler<BezierCurve,ExtrudeSamplerPoint>
    {
        public ExtrudeSampler(string label, Curve3DEditMode editMode,BezierCurve positionCurve) : base(label,editMode) 
        {
            InsertPointAtDistance(0,positionCurve);
        }

        public ExtrudeSampler(ExtrudeSampler objToClone, bool createNewGuids,Curve3D curve) : base(objToClone,createNewGuids,curve) { }
        public override BezierCurve CloneValue(BezierCurve val, bool shouldCreateGuids)
        {
            return new BezierCurve(val, shouldCreateGuids);
        }
#if UNITY_EDITOR
        public override void SelectEdit(Curve3D curve, List<ExtrudeSamplerPoint> selectedPoints, ExtrudeSamplerPoint mainPoint)
        {
            base.SelectEdit(curve, selectedPoints, mainPoint);
            bool oldClosedLoop = mainPoint.value.isClosedLoop;
            bool oldAutomaticTangents = mainPoint.value.automaticTangents;
            float oldTangentSmoothing = mainPoint.value.automaticTangentSmoothing;
            bool newClosedLoop = EditorGUILayout.Toggle("IsClosedLoop",oldClosedLoop);
            bool newAutomaticTangents = EditorGUILayout.Toggle("AutoTangents",oldAutomaticTangents);
            mainPoint.value.isClosedLoop = newClosedLoop;
            mainPoint.value.automaticTangents = newAutomaticTangents;
            bool didSmoothingChange = false;
            if (oldAutomaticTangents)
            {
                float newTangentSmoothing = EditorGUILayout.Slider("AutoTangents",oldTangentSmoothing,BezierCurve.tangentSmoothingMin,1);
                didSmoothingChange = newTangentSmoothing != oldTangentSmoothing;
                mainPoint.value.automaticTangentSmoothing = newTangentSmoothing;
            }
            if (newClosedLoop != oldClosedLoop || newAutomaticTangents != oldAutomaticTangents || didSmoothingChange)
                curve.UICurve.Initialize();
        }
#endif
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
                newPoint.PointGroups[0].SetPositionLocal(PointGroupIndex.RightTangent, new Vector3(.3f, 0, 0));
                newPoint.PointGroups[1].SetPositionLocal(PointGroupIndex.Position, new Vector3(.3f, .3f, 0));
                newPoint.PointGroups[1].SetPositionLocal(PointGroupIndex.LeftTangent, new Vector3(0, .3f, 0));
            }
            newPoint.dimensionLockMode = DimensionLockMode.z;
            newPoint.Recalculate();
            return newPoint;
        }
        ///Secondary curve distance is a value between 0 and 1
        public Vector3 SampleAt(float primaryCurveDistance,float secondaryCurveDistance, BezierCurve primaryCurve,out Vector3 reference,out Vector3 tangent,bool useCachedDistance=false)
        {
            //This needs to interpolate references smoothly
            Vector3 SamplePosition(ExtrudeSamplerPoint point, out Vector3 myRef,out Vector3 myTan)
            {
                var samp = point.value.GetPointAtDistance(secondaryCurveDistance * point.value.GetLength());
                myRef = samp.reference;
                myTan = samp.tangent;
                return samp.position;
            }
            Vector3 InterpolateSamples(ExtrudeSamplerPoint lowerCurve, ExtrudeSamplerPoint upperCurve,float lowerDistance,float upperDistance,out Vector3 interpolatedReference,out Vector3 interpolatedTangent)
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
            float previousDistance = availableCurves[0].GetDistance(primaryCurve,useCachedDistance);
            if (availableCurves.Count==1 || (previousDistance > primaryCurveDistance && !primaryCurve.isClosedLoop))
                return SamplePosition(availableCurves[0], out reference,out tangent);
            if (previousDistance > primaryCurveDistance && primaryCurve.isClosedLoop)

            {
                var lower = availableCurves[availableCurves.Count - 1];
                var upper = availableCurves[0];
                var lowerDistance = lower.GetDistance(primaryCurve,useCachedDistance)-primaryCurve.GetLength();
                var upperDistance = upper.GetDistance(primaryCurve,useCachedDistance);
                return InterpolateSamples(lower,upper,lowerDistance,upperDistance,out reference,out tangent);
            }
            ExtrudeSamplerPoint previousCurve = availableCurves[0];
            for (int i = 1; i < availableCurves.Count; i++)
            {
                var currCurve = availableCurves[i];
                float currentDistance = currCurve.GetDistance(primaryCurve,useCachedDistance);
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
                var lowerDistance = lower.GetDistance(primaryCurve,useCachedDistance);
                var upperDistance = upper.GetDistance(primaryCurve,useCachedDistance)+primaryCurve.GetLength();
                return InterpolateSamples(lower,upper,lowerDistance,upperDistance,out reference,out tangent);
            }
        }
    }
}
