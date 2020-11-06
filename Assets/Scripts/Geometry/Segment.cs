using System.Collections.Generic;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    [System.Serializable]
    public class Segment
    {
        public List<PointOnCurve> samples = new List<PointOnCurve>();
        public float length = 0;
        /// <summary>
        /// Cummulative length including current segment
        /// </summary>
        public float cummulativeLength = -1;
        public Segment(BezierCurve owner, int segmentIndex, bool isLastSegment)
        {
            Recalculate(owner, segmentIndex, isLastSegment);
        }
        public void Recalculate(BezierCurve owner, int segmentIndex, bool isLastSegment)
        {
            bool shouldPerformOneLessSample = isLastSegment && owner.isClosedLoop;
            samples.Clear();
            float len = 0;
            Vector3 previousPosition = owner.GetSegmentPositionAtTime(segmentIndex, 0.0f);
            AddLength(segmentIndex, 0.0f, 0, previousPosition, owner.GetSegmentTangentAtTime(segmentIndex, 0.0f));
            int numSamplesMinusOne = owner.owner.samplesPerSegment - 1;
            for (int i = 1; i <= numSamplesMinusOne; i++)//we include the end point with <=
            {
                var time = i / (float)numSamplesMinusOne;
                Vector3 currentPosition = owner.GetSegmentPositionAtTime(segmentIndex, time);
                var dist = Vector3.Distance(currentPosition, previousPosition);
                len += dist;
                AddLength(segmentIndex, time, len, currentPosition, owner.GetSegmentTangentAtTime(segmentIndex, time));
                previousPosition = currentPosition;
            }
            this.length = len;
        }
        public Segment(Segment objToClone)
        {
            this.cummulativeLength = objToClone.cummulativeLength;
            this.length = objToClone.length;
            this.samples = new List<PointOnCurve>(objToClone.samples.Count);
            foreach (var i in objToClone.samples)
                samples.Add(new PointOnCurve(i));
        }
        public void AddLength(int segmentIndex, float time, float length, Vector3 position, Vector3 tangent)
        {
            samples.Add(new PointOnCurve(time, length, position, segmentIndex, tangent));
        }
        public float GetTimeAtLength(float length, out PointOnCurve lowerPoint, out Vector3 lowerReference)
        {
            if (length < 0 || length > this.length)
                throw new System.ArgumentException("Length out of bounds");
            PointOnCurve previousPoint = samples[0];
            if (previousPoint.distanceFromStartOfSegment > length)
                throw new System.Exception("Should always have a point at 0.0");
            for (int i = 1; i < samples.Count; i++)
            {
                var currentPoint = samples[i];
                if (currentPoint.distanceFromStartOfSegment > length)
                {
                    float fullPieceLength = currentPoint.distanceFromStartOfSegment - previousPoint.distanceFromStartOfSegment;
                    float partialPieceLength = length - previousPoint.distanceFromStartOfSegment;
                    lowerPoint = previousPoint;
                    lowerReference = lowerPoint.reference;
                    return Mathf.Lerp(previousPoint.time, currentPoint.time, partialPieceLength / fullPieceLength);
                }
                previousPoint = currentPoint;
            }
            lowerPoint = samples[samples.Count - 2];
            lowerReference = lowerPoint.reference;
            return samples[samples.Count - 1].time;
        }
        public float GetDistanceAtTime(float time)
        {
            if (time < 0 || time > 1.0f)
                throw new System.ArgumentException("Length out of bounds");
            PointOnCurve previousPoint = samples[0];
            if (previousPoint.time > time)
                throw new System.Exception("Should always have a point at 0.0");
            for (int i = 1; i < samples.Count; i++)
            {
                var currentPoint = samples[i];
                if (currentPoint.time > time)
                {
                    float fullPieceTime = currentPoint.time - previousPoint.time;
                    float partialPieceTime = time - previousPoint.time;
                    return Mathf.Lerp(previousPoint.distanceFromStartOfSegment, currentPoint.distanceFromStartOfSegment, partialPieceTime / fullPieceTime);
                }
                previousPoint = currentPoint;
            }
            return samples[samples.Count - 1].distanceFromStartOfSegment;
        }

    }
}
