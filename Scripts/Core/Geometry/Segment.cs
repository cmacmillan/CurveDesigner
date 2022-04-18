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
            if (samples[0].distanceFromStartOfSegment > length)
                throw new System.Exception("Should always have a point at 0.0");
            //binary search
            int bottom = 0;//inclusive
            int top = samples.Count-1;//inclusive
            while (top-bottom>1)
            {
                int middle = (bottom + top) / 2;
                var middlePoint = samples[middle];
                if (middlePoint.distanceFromStartOfSegment >= length)
                    top = middle;
                else
                    bottom = middle;
            }
            var nextPoint = samples[top];
            var previousPoint = samples[bottom];
            float fullPieceLength = nextPoint.distanceFromStartOfSegment - previousPoint.distanceFromStartOfSegment;
            float partialPieceLength = length - previousPoint.distanceFromStartOfSegment;
            lowerPoint = previousPoint;
            lowerReference = lowerPoint.reference;
            return Mathf.Lerp(previousPoint.time, nextPoint.time, partialPieceLength / fullPieceLength);
        }
        public float GetDistanceAtTime(float time)
        {
            if (time < 0 || time > 1.0f)
                throw new System.ArgumentException("Length out of bounds");
            if (samples[0].time > time)
                throw new System.Exception("Should always have a point at 0.0");
            //binary search
            int bottom = 0;//inclusive
            int top = samples.Count-1;//inclusive
            while (top-bottom>1)
            {
                int middle = (bottom + top) / 2;
                var middlePoint = samples[middle];
                if (middlePoint.time >= time)
                    top = middle;
                else
                    bottom = middle;
            }
            var nextPoint = samples[top];
            var previousPoint = samples[bottom];
            float fullPieceTime = nextPoint.time - previousPoint.time;
            float partialPieceTime = time - previousPoint.time;
            return Mathf.Lerp(previousPoint.distanceFromStartOfSegment, nextPoint.distanceFromStartOfSegment, partialPieceTime / fullPieceTime); 
        }

    }
}
