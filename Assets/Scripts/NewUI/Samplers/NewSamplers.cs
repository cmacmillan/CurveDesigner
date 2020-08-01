using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewSamplers
{
    [System.Serializable]
    public struct DistanceAlongCurve
    {
        public float time;
        public int segmentIndex;
        public float GetDistance(BezierCurve curve)
        {
            return curve.GetDistanceAtSegmentIndexAndTime(segmentIndex,time);
        }
        public void SetDistance(float distance,BezierCurve curve, bool shouldSort)
        {
            var point = curve.GetPointAtDistance(distance);
            time = point.time;
            segmentIndex = point.segmentIndex;
            //if (shouldSort)
                //thingToSort.
        }
    }
    //public class I
    public interface IDistanceSamplerData
    {
        DistanceAlongCurve DistanceAlongCurve { get; set; }
    }
    public interface IDistanceSamplerData<T> : IDistanceSamplerData
    {
        T Value { get; set; }
        T Lerp(T start, T end, float time);
    }

    [System.Serializable]
    public struct ColorSamplerPoint : IDistanceSamplerData<Color>
    {
        public DistanceAlongCurve distanceAlongCurve;
        public Color value;

        DistanceAlongCurve IDistanceSamplerData.DistanceAlongCurve { get => distanceAlongCurve; set => distanceAlongCurve = value; }
        Color IDistanceSamplerData<Color>.Value { get => value; set => this.value = value; }

        public Color Lerp(Color start, Color end, float time)
        {
            return Color.Lerp(start, end, time);
        }
    }
    /*
    public interface ISamplerPoint : ISelectable
    {
        float Time { get; set; }
        int SegmentIndex { get; set; }
        void SetDistance(float distance,BezierCurve curve,bool shouldSort=true);
    }
    public interface IDistanceSampler : IActiveElement
    {
        IEnumerable<ISamplerPoint> GetPoints(BezierCurve curve);
        IEnumerable<ISamplerPoint> AllPoints();
        void RecalculateOpenCurveOnlyPoints(BezierCurve curve);
        void Sort(BezierCurve curve);
        int InsertPointAtDistance(float distance,BezierCurve curve);
    }
    */
}
