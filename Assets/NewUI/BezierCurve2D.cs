///NOTE: THIS FILE ISN'T FINISHED AND DOESN'T WORK
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

namespace Assets.NewUI
{
    public interface I2DCurveInfoProvider
    {
        float GetLength();
        bool IsClosedLoop();
    }
    public class Segment2DSample
    {
        public Segment2DSample(Vector2 position,float time)
        {
            this.position = position;
            this.time = time;
        }
        public Vector2 position;
        public float time;
    }
    public class Segment2D
    {
        private const int numSamples = 10;
        public List<Segment2DSample> samples = new List<Segment2DSample>();
        public Segment2D(BezierCurve2D owner, int segmentIndex)
        {
            for (int i=0;i<numSamples;i++)
            {
                float time = i / (float)numSamples;
                samples.Add(new Segment2DSample(owner.SegmentPositionAtTime(segmentIndex,time),time));
            }
        }
    }
    public struct WeightAndTangent
    {
        [Range(0, 1)]
        public float weight;
        public float tangent;
        public WeightAndTangent(float weight,float tangent)
        {
            this.weight = weight;
            this.tangent = tangent;
        }
    }
    public class PointGroup2D
    {
        public bool isGroupLocked = true;
        public Vector2 position;
        public WeightAndTangent left;
        public WeightAndTangent right;
    }
    public class BezierCurve2D
    {
        private class TestInfoProvider : I2DCurveInfoProvider
        {
            public TestInfoProvider(bool isClosedLoop,float length)
            {
                this.isClosedLoop = isClosedLoop;
                this.length = length;
            }
            private bool isClosedLoop;
            private float length;
            public float GetLength() { return length; }
            public bool IsClosedLoop() { return isClosedLoop; }
        }
        public static void Test()
        {
            {
                BezierCurve2D curve = new BezierCurve2D(new TestInfoProvider(false,10.0f));
                var group1 = new PointGroup2D();
                group1.position = new Vector2(1, 1);
                group1.right = new WeightAndTangent(0, 0);
                curve.pointGroups.Add(group1);
                var group2 = new PointGroup2D();
                group2.position = new Vector2(2, 0);
                group2.left = new WeightAndTangent(0, 0);
                curve.pointGroups.Add(group2);
                curve.Recalculate();
                Assert.AreApproximatelyEqual(curve.GetY(1.5f), 0.5f);
            }
            {
                BezierCurve2D curve = new BezierCurve2D(new TestInfoProvider(false,10.0f));
                var group1 = new PointGroup2D();
                group1.position = new Vector2(1, 1);
                group1.right = new WeightAndTangent(.5f, 0);
                curve.pointGroups.Add(group1);
                var group2 = new PointGroup2D();
                group2.position = new Vector2(2, 0);
                group2.left = new WeightAndTangent(.5f, 0);
                curve.pointGroups.Add(group2);
                curve.Recalculate();
                Assert.AreApproximatelyEqual(curve.GetY(1),1);
                Assert.AreApproximatelyEqual(curve.GetY(-10),1);
                Assert.AreApproximatelyEqual(curve.GetY(3),0);
                Assert.AreApproximatelyEqual(curve.GetY(13),0);
                Assert.AreApproximatelyEqual(curve.GetY(2),0);
                Assert.AreApproximatelyEqual(curve.GetY(1.5f), 0.5f);
            }
            {
                BezierCurve2D curve = new BezierCurve2D(new TestInfoProvider(true,2.0f));
                var group1 = new PointGroup2D();
                group1.position = new Vector2(0, 0);
                group1.right = new WeightAndTangent(0, 0);
                curve.pointGroups.Add(group1);
                var group2 = new PointGroup2D();
                group2.position = new Vector2(1, 1);
                group2.left = new WeightAndTangent(0, 0);
                curve.pointGroups.Add(group2);
                curve.Recalculate();
                Assert.AreApproximatelyEqual(curve.GetY(.5f), 0.5f);
                Assert.AreApproximatelyEqual(curve.GetY(1.5f), 0.5f);
            }
        }


        public int NumSegments { get { return _infoProvider.IsClosedLoop()?pointGroups.Count:pointGroups.Count-1; } }
        public I2DCurveInfoProvider _infoProvider;
        List<PointGroup2D> pointGroups = new List<PointGroup2D>();
        private List<Segment2D> _segments = new List<Segment2D>();
        public Vector2 GetWorldPositionByIndex(PGIndex index,int pointGroupIndex)
        {
            var center = pointGroups[pointGroupIndex];
            switch (index)
            {
                case PGIndex.LeftTangent:
                    {
                        float segmentLength = SegmentXLength(PGIndex.LeftTangent, pointGroupIndex);
                        float xOffset = -segmentLength * center.left.weight;
                        float yOffset = xOffset * center.left.tangent;
                        return center.position + new Vector2(xOffset, yOffset);
                    }
                case PGIndex.RightTangent:
                    {
                        float segmentLength = SegmentXLength(PGIndex.RightTangent, pointGroupIndex);
                        float xOffset = segmentLength * center.right.weight;
                        float yOffset = xOffset * center.right.tangent;
                        return center.position + new Vector2(xOffset,yOffset);
                    }
                case PGIndex.Position:
                default:
                    return center.position;
            }
        }
        public void SetWorldPositionByIndex(PGIndex index,Vector2 position,int pointGroupIndex)
        {
            var center = pointGroups[pointGroupIndex];
            float centerX = mod(center.position.x, _infoProvider.GetLength());
            float centerY = mod(center.position.y, _infoProvider.GetLength());
            switch (index)
            {
                case PGIndex.LeftTangent:
                    {
                        float segmentLength = SegmentXLength(PGIndex.LeftTangent, pointGroupIndex);
                        float xOffset = centerX - position.x;
                        float yOffset = centerY - position.y;
                        center.left.weight = Mathf.Clamp01(xOffset/segmentLength);
                        center.left.tangent = yOffset / xOffset;
                    }
                    break;
                case PGIndex.RightTangent:
                    {
                        float segmentLength = SegmentXLength(PGIndex.RightTangent, pointGroupIndex);
                        float xOffset = position.x - centerX;
                        float yOffset = position.y - centerY;
                        center.right.weight = Mathf.Clamp01(xOffset / segmentLength);
                        center.right.tangent = yOffset / xOffset;
                    }
                    break;
                case PGIndex.Position:
                    center.position = position;
                    break;
            }
        }
        private float SegmentXLength(PGIndex index,int pointGroupIndex)
        {
            var center = pointGroups[pointGroupIndex];
            float centerX = mod(center.position.x, _infoProvider.GetLength());
            switch (index)
            {
                case PGIndex.LeftTangent:
                    {
                        var lower = pointGroups[mod(pointGroupIndex - 1, pointGroups.Count)];
                        float lowerX = mod(center.position.x, _infoProvider.GetLength());
                        float segmentLength = centerX - lowerX;
                        return segmentLength;
                    }
                case PGIndex.RightTangent:
                    {
                        var upper = pointGroups[mod(pointGroupIndex + 1, pointGroups.Count)];
                        float upperX = mod(upper.position.x, _infoProvider.GetLength());
                        float segmentLength = upperX - centerX;
                        return segmentLength;
                    }
                default:
                    throw new InvalidOperationException();
            }
        }
        float mod(float x, float m)
        {
            return (x % m + m) % m;
        }
        int mod(int x, int m)
        {
            return (x % m + m) % m;
        }
        public void Recalculate()
        {
            _segments.Clear();
            for (int i=0;i<NumSegments;i++)
                _segments.Add(new Segment2D(this,i));

        }
        public BezierCurve2D(I2DCurveInfoProvider infoProvider)
        {
            _infoProvider = infoProvider;
        }
        public float GetY(float x)
        {
            if (_infoProvider.IsClosedLoop())
                x = mod(x, _infoProvider.GetLength());
            else
                x = Mathf.Clamp(x, 0, _infoProvider.GetLength());
            GetSegmentIndexAtTimeAtX(x, out int segmentIndex, out float time);
            return SegmentPositionAtTime(segmentIndex,time).y;
        }
        private void GetSegmentIndexAtTimeAtX(float x,out int segmentIndex, out float time)
        {
            if (x < pointGroups[0].position.x)
                throw new ArgumentException();
            int index;
            for (index = 0; index < pointGroups.Count; index++)
                if (pointGroups[index].position.x > x)
                    break;
            segmentIndex = index - 1;
            var segment = _segments[segmentIndex];
            int sampleIndex = 0;
            for (sampleIndex = 0; sampleIndex < segment.samples.Count; sampleIndex++)
                if (segment.samples[sampleIndex].position.x > x)
                    break;
            Segment2DSample lower = segment.samples[sampleIndex - 1];
            Segment2DSample upper = segment.samples[sampleIndex];
            float lerp = (x - lower.position.x)/(upper.position.x - lower.position.x);
            time=Mathf.Lerp(lower.time,upper.time,lerp);
        }
        public Vector2 SegmentPositionAtTime(int segmentIndex,float time)
        {
            var start = GetWorldPositionByIndex(PGIndex.Position, segmentIndex);
            var startTangent = GetWorldPositionByIndex(PGIndex.RightTangent, segmentIndex);
            Vector2 endTangent;
            Vector2 end;
            if (segmentIndex == pointGroups.Count-1)
            {
                endTangent = GetWorldPositionByIndex(PGIndex.LeftTangent, 0);
                end = GetWorldPositionByIndex(PGIndex.LeftTangent, 0);
                //undeflow these values that wrapped
                endTangent.x = endTangent.x + _infoProvider.GetLength();
                end.x = end.x+_infoProvider.GetLength();
            }
            else
            {
                endTangent = GetWorldPositionByIndex(PGIndex.LeftTangent,segmentIndex+1);
                end = GetWorldPositionByIndex(PGIndex.Position,segmentIndex+1);
            }
            return Solve(start, startTangent, endTangent, end, time);
        }
        private Vector2 Solve(Vector2 start, Vector2 startTangent,Vector2 endTangent, Vector2 end,float time)
        {
            Vector2 SolveDown(Vector2 _1,Vector2 _2,Vector2 _3)
            {
                return Vector2.Lerp(Vector2.Lerp(_1, _2, time), Vector2.Lerp(_2, _3, time), time);
            }
            return Vector2.Lerp(SolveDown(start,startTangent,endTangent),SolveDown(startTangent,endTangent,end), time);
        }
    }
}
