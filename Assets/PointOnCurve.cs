using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class PointOnCurve : ISegmentTime
{
    public PointOnCurve(PointOnCurve pointToClone)
    {
        this.time = pointToClone.time;
        this.distanceFromStartOfSegment = pointToClone.distanceFromStartOfSegment;
        this.position = pointToClone.position;
        this.distanceFromStartOfCurve = pointToClone.distanceFromStartOfCurve;
        this.segmentIndex = pointToClone.segmentIndex;
    }
    public PointOnCurve(float time, float distanceFromStartOfSegment, Vector3 position,int segmentIndex)
    {
        this.time = time;
        this.distanceFromStartOfSegment = distanceFromStartOfSegment;
        this.position = position;
        this.segmentIndex = segmentIndex;
    }
    public int segmentIndex;
    public float time;

    public Vector3 position;
    /// <summary>
    /// Distance from start of segment
    /// </summary>
    public float distanceFromStartOfSegment;
    public float distanceFromStartOfCurve;

    public int SegmentIndex { get { return segmentIndex; } }

    public float Time { get { return time; } }
}
public interface ISegmentTime
{
    int SegmentIndex { get; } 
    float Time { get; }
}
