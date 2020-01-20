﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Segment
{
    private const int _numSegmentLengthSamples = 10;
    public Segment(BeizerCurve owner,int segmentIndex)
    {
        float len = 0;
        Vector3 previousPosition = owner.GetSegmentPositionAtTime(segmentIndex,0.0f);
        for (int i = 1; i < _numSegmentLengthSamples; i++)
        {
            var time = i / (float)_numSegmentLengthSamples;//We really need to ensure this includes the end points
            Vector3 currentPosition = owner.GetSegmentPositionAtTime(segmentIndex, time);
            len += Vector3.Distance(currentPosition, previousPosition);
            AddLength(segmentIndex,time,len,currentPosition,owner.segments[segmentIndex-1].cummulativeLength+len);
            previousPosition = currentPosition; 
        }
    }
    public Segment(Segment objToClone)
    {
        throw new System.NotImplementedException();
    }
    public List<PointOnCurve> samples = new List<PointOnCurve>();
    public float length = 0;
    /// <summary>
    /// Cummulative length including current segment
    /// </summary>
    public float cummulativeLength=-1;
    public void AddLength(int segmentIndex,float time, float length, Vector3 position, float distanceAlongCurve)
    {
        samples.Add(new PointOnCurve(time, length, position, distanceAlongCurve,segmentIndex));
        this.length += length;
    }
    public float GetTimeAtLength(float length)
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
                return Mathf.Lerp(previousPoint.time, currentPoint.time, partialPieceLength / fullPieceLength);
            }
            previousPoint = currentPoint;
        }
        throw new System.Exception("Should always have a point at 1.0");
    }
    public float GetLengthAtTime(float time)
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
        throw new System.Exception("Should always have a point at 1.0");
    }

}
