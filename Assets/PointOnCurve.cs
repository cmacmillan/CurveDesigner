using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class PointOnCurve
{
    public PointOnCurve(float time, float distanceFromStartOfSegment, Vector3 position, float distanceFromStartOfCurve,int segmentIndex)
    {
        this.time = time;
        this.distanceFromStartOfSegment = distanceFromStartOfSegment;
        this.position = position;
        this.distanceFromStartOfCurve = distanceFromStartOfCurve;
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
}
