using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.NewUI
{
    [System.Serializable]
    public class FloatDistanceSampler : ValueDistanceSampler<float, FloatSamplerPoint,FloatDistanceSampler>
    {
        public FloatDistanceSampler(string fieldDisplayName,float defaultValue): base(fieldDisplayName)
        {
            constValue = defaultValue;
        }
        public FloatDistanceSampler(FloatDistanceSampler objToClone) : base(objToClone) { }

        protected override float CloneValue(float value)
        {
            return value;
        }

        public override float Lerp(float val1, float val2, float lerp) { return Mathf.Lerp(val1,val2,lerp); }

        public float GetDistanceByAreaUnderInverseCurve(float targetAreaUnderCurve, bool isClosedLoop, float curveLength, BezierCurve curve)
        {
            var pointsInsideCurve = GetPoints(curve);
            if (pointsInsideCurve.Count == 0)
                return targetAreaUnderCurve / constValue;
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

    [System.Serializable]
    public class FloatSamplerPoint : FieldEditableSamplerPoint<float,FloatSamplerPoint,FloatDistanceSampler> 
    {
        public override float Field(string displayName, float originalValue)
        {
            return EditorGUILayout.FloatField(displayName, originalValue);
        }

        public override float Add(float v1, float v2) { return v1 + v2; }

        public override float Subtract(float v1, float v2) { return v1 - v2; }

        public override float Zero() { return 0; }

        public override float CloneValue(float value)
        {
            return value;
        }
    }
}
