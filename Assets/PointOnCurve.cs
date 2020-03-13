﻿using System.Collections;
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
        this.tangent = pointToClone.tangent;
        this.reference = pointToClone.reference;
    }
    public PointOnCurve(float time, float distanceFromStartOfSegment, Vector3 position,int segmentIndex,Vector3 tangent)
    {
        this.time = time;
        this.distanceFromStartOfSegment = distanceFromStartOfSegment;
        this.position = position;
        this.segmentIndex = segmentIndex;
        this.tangent = tangent;
    }
    public int segmentIndex;
    public float time;
    public Vector3 tangent;
    public Vector3 reference;

    public void CalculateReference(PointOnCurve previousPoint, Vector3 previousReference)
    {
        Vector3 DoubleReflectionRMF(Vector3 x0, Vector3 x1, Vector3 t0, Vector3 t1, Vector3 r0)
        {
            Vector3 v1 = x1 - x0;
            float c1 = Vector3.Dot(v1, v1);
            Vector3 rL = r0 - (2.0f / c1) * Vector3.Dot(v1, r0) * v1;
            Vector3 tL = t0 - (2.0f / c1) * Vector3.Dot(v1, t0) * v1;
            Vector3 v2 = t1 - tL;
            float c2 = Vector3.Dot(v2, v2);
            return rL - (2.0f / c2) * Vector3.Dot(v2, rL) * v2;
        }
        reference=DoubleReflectionRMF(previousPoint.position, this.position, previousPoint.tangent.normalized, this.tangent.normalized, previousReference);
    }

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
