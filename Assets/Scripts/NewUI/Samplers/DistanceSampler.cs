using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    [System.Serializable]
    public struct SegmentDistance
    {
        public int segmentIndex;
        public float time;
    }

    //Not serializable
    public class SamplerPoint<T>
    {
        public T value;
        public SegmentDistance segmentDistance;
        public SamplerPoint(T value, SegmentDistance segmentDistance){
            this.value = value;
            this.segmentDistance = segmentDistance;
        }
    }

    //Not serializable
    public class DistanceSampler<T>
    {
        public List<SamplerPoint<T>> points;
        public DistanceSampler(List<T> items,List<SegmentDistance> distances){
            if (items.Count != distances.Count)
                throw new ArgumentException("Items and distances must have same Count.");
            points = new List<SamplerPoint<T>>();
            for (int i=0;i<items.Count;i++)
                points.Add(new SamplerPoint<T>(items[i],distances[i]));
        }
        
    }
}
