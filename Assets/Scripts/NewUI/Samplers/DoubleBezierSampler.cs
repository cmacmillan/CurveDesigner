using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    [System.Serializable]
    public class BezierCurveDistanceValue : CurveTrackingDistance
    {
        public BezierCurve curve;
    }
    [System.Serializable]
    public class DoubleBezierSampler
    {
        public List<BezierCurveDistanceValue> curves = new List<BezierCurveDistanceValue>();
        ///Secondary curve distance is a value between 0 and 1
        public Vector3 SampleAt(float primaryCurveDistance,float secondaryCurveDistance, BezierCurve primaryCurve)
        {
            if (curves.Count == 0)
                return Vector3.zero;
            Vector3 SamplePosition(BezierCurveDistanceValue value)
            {
                return value.curve.GetPointAtDistance(secondaryCurveDistance*value.curve.GetLength()).position;
            }
            float previousDistance = curves[0].GetDistance(primaryCurve);
            if (previousDistance > primaryCurveDistance)
                return SamplePosition(curves[0]);
            BezierCurveDistanceValue lowerCurve = curves[0];
            for (int i = 1; i < primaryCurveDistance; i++)
            {
                var curr = curves[i];
                float currentDistance = curr.GetDistance(primaryCurve);
                if (currentDistance > primaryCurveDistance)
                {
                    float distanceBetweenSegments = currentDistance - previousDistance;
                    float lerpVal = (primaryCurveDistance - previousDistance) / distanceBetweenSegments;
                    Vector3 lowerPosition = SamplePosition(lowerCurve);
                    Vector3 upperPosition = SamplePosition(curr);
                    return Vector3.Lerp(lowerPosition,upperPosition,lerpVal);
                }
                previousDistance = currentDistance;
                lowerCurve = curr;
            }
            return SamplePosition(curves[curves.Count-1]);
        }
    }
}
