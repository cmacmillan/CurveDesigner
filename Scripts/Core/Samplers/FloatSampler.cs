using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    [System.Serializable]
    public class FloatSamplerPoint : SamplerPoint<float,FloatSamplerPoint> { }
    [System.Serializable]
    public class FloatSampler : ValueSampler<float,FloatSamplerPoint>
    {
        public float minValue;
        public float maxValue;
        public FloatSampler(string label,float defaultValue,Curve3DEditMode editMode,float minValue = float.NegativeInfinity, float maxValue = float.PositiveInfinity): base(label, editMode)
        {
            constValue = defaultValue;
            this.minValue = minValue;
            this.maxValue = maxValue;
        }
        public FloatSampler(FloatSampler objToClone, bool createNewGuids,Curve3D curve) : base(objToClone,createNewGuids,curve) {
            this.minValue = objToClone.minValue;
            this.maxValue = objToClone.maxValue;
        }
        private float Constrain(float f)
        {
            return Mathf.Clamp(f,minValue,maxValue);
        }
#if UNITY_EDITOR
        public override void ConstantField(Rect rect)
        {
            constValue = Constrain(EditorGUI.FloatField(rect, GetLabel(), constValue));
        }
        public override void SelectEdit(Curve3D curve, List<FloatSamplerPoint> selectedPoints,FloatSamplerPoint mainPoint)
        {
            float originalValue = mainPoint.value;
            EditorGUIUtility.SetWantsMouseJumping(1);
            float fieldVal=EditorGUILayout.FloatField(GetLabel(), originalValue);
            float valueOffset = fieldVal-originalValue;
            float minChange = float.MaxValue;
            foreach (var i in selectedPoints)
            {
                float newVal = Constrain(i.value + valueOffset);
                float change = newVal - i.value;
                if (Mathf.Abs(change) < Mathf.Abs(minChange))
                    minChange = change;
            }
            base.SelectEdit(curve, selectedPoints,mainPoint);
            if (minChange == 0)
                return;
            foreach (var target in selectedPoints)
                target.value = target.value + minChange;
        }
#endif

        public override float Lerp(float val1, float val2, float lerp)
        {
            return Mathf.Lerp(val1, val2, lerp);
        }

        public float GetDistanceByAreaUnderInverseCurve(float targetAreaUnderCurve, bool isClosedLoop, float curveLength, BezierCurve curve)
        {
            var pointsInsideCurve = GetPoints(curve);
            if (pointsInsideCurve.Count == 0 || !UseKeyframes)
            {
                return targetAreaUnderCurve*constValue;
            }
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
            float GetVal(FloatSamplerPoint val)
            {
                return 1.0f / (val.value);
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
    }
}
