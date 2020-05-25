﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//A class which defines a chain of 3rd order bezier curves (4 control points per segment)
[System.Serializable]
public partial class BezierCurve
{
    [SerializeField]
    [HideInInspector]
    public List<PointGroup> PointGroups;
    public int NumControlPoints { get { return isClosedLoop?PointGroups.Count*3:PointGroups.Count*3-2; } }
    public int NumSegments { get { return isClosedLoop?PointGroups.Count:PointGroups.Count-1; } }
    public bool placeLockedPoints = true;
    public bool isCurveOutOfDate = true;
    public SplitInsertionNeighborModification splitInsertionBehaviour;
    public Curve3D owner;
    public bool isClosedLoop;
    [System.NonSerialized]
    [HideInInspector]
    public List<Segment> segments = null;

    private Vector3 GetTangent(int index)
    {
        return (this[index + 1] - this[index]);//.normalized;
    }
    
    public BezierCurve() { }
    public BezierCurve(BezierCurve curveToClone)
    {
        PointGroups = new List<PointGroup>();
        foreach (var i in curveToClone.PointGroups)
        {
            PointGroups.Add(new PointGroup(i));
        }
        this.isClosedLoop = curveToClone.isClosedLoop;
        this.segments = new List<Segment>(curveToClone.segments.Count);
        foreach (var i in curveToClone.segments)
            segments.Add(new Segment(i));
        this.placeLockedPoints = curveToClone.placeLockedPoints;
        this.isCurveOutOfDate = curveToClone.isCurveOutOfDate;
        this.splitInsertionBehaviour = curveToClone.splitInsertionBehaviour;
    }

    public enum SplitInsertionNeighborModification
    {
        DoNotModifyNeighbors=0,
        RetainCurveShape=1,
    }

    #region curve manipulation
    public void Initialize()
    {
        PointGroups = new List<PointGroup>();
        var pointA = new PointGroup(placeLockedPoints);
        pointA.SetWorldPositionByIndex(PGIndex.Position, Vector3.zero);
        pointA.SetWorldPositionByIndex(PGIndex.LeftTangent, new Vector3(0,1,0));
        pointA.SetWorldPositionByIndex(PGIndex.RightTangent, new Vector3(1,0,0));
        PointGroups.Add(pointA);
        var pointB = new PointGroup(placeLockedPoints);
        pointB.SetWorldPositionByIndex(PGIndex.Position, new Vector3(1,1,0));
        pointB.SetWorldPositionByIndex(PGIndex.LeftTangent, new Vector3(0,1,0));
        pointB.SetWorldPositionByIndex(PGIndex.RightTangent, new Vector3(1,0,0));
        PointGroups.Add(pointB);
    }

    public int InsertSegmentAfterIndex(ISegmentTime splitPoint,bool lockPlacedPoint,SplitInsertionNeighborModification shouldModifyNeighbors)
    {
        var prePointGroup = PointGroups[splitPoint.SegmentIndex];
        var postPointGroup = PointGroups[(splitPoint.SegmentIndex + 1)%PointGroups.Count];
        PointGroup point = new PointGroup(lockPlacedPoint);
        var basePosition = this.GetSegmentPositionAtTime(splitPoint.SegmentIndex, splitPoint.Time);
        point.SetWorldPositionByIndex(PGIndex.Position,basePosition);
        Vector3 leftTangent;
        Vector3 rightTangent;
        Vector3 preLeftTangent;
        Vector3 postRightTangent;
        SolvePositionAtTimeTangents(GetVirtualIndex(splitPoint.SegmentIndex, 0), 4, splitPoint.Time, out leftTangent, out rightTangent, out preLeftTangent, out postRightTangent);

        void prePointModify()
        {
            prePointGroup.SetWorldPositionByIndex(PGIndex.RightTangent,preLeftTangent);
        }
        void postPointModify()
        {
            postPointGroup.SetWorldPositionByIndex(PGIndex.LeftTangent,postRightTangent);
        }
        switch (shouldModifyNeighbors)
        {
            case SplitInsertionNeighborModification.RetainCurveShape:
                prePointGroup.SetPointLocked(false);
                postPointGroup.SetPointLocked(false);
                prePointModify();
                postPointModify();
                break;
            default:
                break;
        }

        //use the bigger tangent, this only matters if the point is locked
        if ((leftTangent-point.GetWorldPositionByIndex(PGIndex.Position)).magnitude<(rightTangent-point.GetWorldPositionByIndex(PGIndex.Position)).magnitude)
        {
            point.SetWorldPositionByIndex(PGIndex.LeftTangent, leftTangent);
            point.SetWorldPositionByIndex(PGIndex.RightTangent, rightTangent);
        }
        else
        {
            point.SetWorldPositionByIndex(PGIndex.RightTangent, rightTangent);
            point.SetWorldPositionByIndex(PGIndex.LeftTangent, leftTangent);
        }

        PointGroups.Insert(splitPoint.SegmentIndex+1,point);
        return (splitPoint.SegmentIndex+1);
    }

    public void AddDefaultSegment()
    {
        var finalPointGroup = PointGroups[PointGroups.Count - 1];
        var finalPointPos = finalPointGroup.GetWorldPositionByIndex(PGIndex.Position);
        finalPointGroup.SetWorldPositionByIndex(PGIndex.RightTangent,finalPointPos+new Vector3(1,0,0));
        var pointB = new PointGroup(placeLockedPoints);
        pointB.SetWorldPositionByIndex(PGIndex.Position,finalPointPos+new Vector3(1,1,0));
        pointB.SetWorldPositionByIndex(PGIndex.LeftTangent,finalPointPos+new Vector3(0,1,0));
        PointGroups.Add(pointB);
        Recalculate();
    }
    #endregion

    #region curve calculations
    //.private const float samplesPerUnit = 100.0f;
    private const int MaxSamples = 500;
    private const int samplesPerSegment = 10;
    private float GetAutoCurveDensity(float curveLength)
    {
        return Mathf.Max(curveLength/MaxSamples,curveLength/(samplesPerSegment*NumSegments));
    }

    public float GetDistanceAtSegmentIndexAndTime(int segmentIndex, float time)
    {
        var segmentLen = segments[segmentIndex].GetDistanceAtTime(time);
        if (segmentIndex > 0)
            return segments[segmentIndex - 1].cummulativeLength + segmentLen;
        return segmentLen;
    }

    public PointOnCurve GetPointAtDistance(float distance)
    {
        distance = Mathf.Clamp(distance, 0, GetLength());
        float remainingDistance= distance;
        for (int segmentIndex=0;segmentIndex<NumSegments;segmentIndex++)
        {
            if (remainingDistance < segments[segmentIndex].length)
            {
                float time = segments[segmentIndex].GetTimeAtLength(remainingDistance,out PointOnCurve lowerPoint,out Vector3 lowerReference);
                Vector3 position = GetSegmentPositionAtTime(segmentIndex, time);
                Vector3 tangent = GetSegmentTangentAtTime(segmentIndex, time);
                var retr = new PointOnCurve(time,remainingDistance,position,segmentIndex,tangent);
                retr.distanceFromStartOfCurve = retr.distanceFromStartOfSegment + (segmentIndex - 1 >= 0 ? segments[segmentIndex - 1].cummulativeLength : 0);
                retr.CalculateReference(lowerPoint,lowerReference);
                return retr;
            }
            remainingDistance-= segments[segmentIndex].length;
        }
        {
            int finalSegmentIndex = NumSegments - 1;
            float time = 1.0f;
            Vector3 position = GetSegmentPositionAtTime(finalSegmentIndex, time);
            Vector3 tangent = GetSegmentTangentAtTime(finalSegmentIndex, time);
            var retr = new PointOnCurve(time, segments[finalSegmentIndex].length, position, finalSegmentIndex,tangent);
            retr.distanceFromStartOfCurve = retr.distanceFromStartOfSegment + (finalSegmentIndex - 1 >= 0 ? segments[finalSegmentIndex - 1].cummulativeLength : 0);
            var finalSegmentSamples = segments[finalSegmentIndex].samples;
            retr.reference = finalSegmentSamples[finalSegmentSamples.Count - 1].reference;
            return retr;
        }
    }

    public void SolvePositionAtTimeTangents(int startIndex, int length, float time, out Vector3 leftTangent, out Vector3 rightTangent, out Vector3 preLeftTangent, out Vector3 postRightTangent)
    {
        leftTangent = SolvePositionAtTime(startIndex,length-1,time);
        rightTangent = SolvePositionAtTime(startIndex+1,length-1,time);

        preLeftTangent = SolvePositionAtTime(startIndex,length-2,time);
        postRightTangent = SolvePositionAtTime(startIndex+2,length-2,time);
    }

    public Vector3 GetSegmentPositionAtTime(int segmentIndex,float time)
    {
        return SolvePositionAtTime(GetVirtualIndex(segmentIndex,0),4,time);
    }
    public Vector3 GetSegmentTangentAtTime(int segmentIndex, float time)
    {
        return SolveTangentAtTime(GetVirtualIndex(segmentIndex,0),3,time).normalized;
    }
    private Vector3 SolveTangentAtTime(int startIndex, int length, float time)
    {
        if (length==2)
            return Vector3.Lerp(GetTangent(startIndex), GetTangent(startIndex + 1), time);
        Vector3 firstHalf = SolveTangentAtTime(startIndex, length - 1, time);
        Vector3 secondHalf = SolveTangentAtTime(startIndex + 1, length - 1, time);
        return Vector3.Lerp(firstHalf, secondHalf, time);
    }

    private Vector3 SolvePositionAtTime(int startIndex, int length, float time)
    {
        if (length == 2)
            return Vector3.Lerp(this[startIndex], this[startIndex + 1], time);
        Vector3 firstHalf = SolvePositionAtTime(startIndex, length - 1, time);
        Vector3 secondHalf = SolvePositionAtTime(startIndex + 1, length - 1, time);
        return Vector3.Lerp(firstHalf, secondHalf, time);
    }
    #endregion

    #region point locking
    public void SetPointLockState(int segmentIndex, int pointIndex,bool state)
    {
        SetPointLockState(GetVirtualIndex(segmentIndex,pointIndex),state);
    }
    public bool GetPointLockState(int segmentIndex, int pointIndex)
    {
        return GetPointLockState(GetVirtualIndex(segmentIndex,pointIndex));
    }
    public void SetPointLockState(int index,bool state)
    {
        PointGroups[GetPointGroupIndex(index)].SetPointLocked(state);
    }
    public bool GetPointLockState(int index)
    {
        return PointGroups[GetPointGroupIndex(index)].GetIsPointLocked();
    }
    #endregion


    #region length calculation
    public float GetLength()
    {
        return segments[NumSegments - 1].cummulativeLength;
    }
    public static Vector3 GetDefaultReferenceVector(Vector3 tangent)
    {
        return NormalTangent(tangent, Vector3.up);
    }
    /// <summary>
    /// must call after modifying points
    /// </summary>
    public void Recalculate()
    {
        if (segments == null)
            segments = new List<Segment>();
        else
            segments.Clear();
        for (int i = 0; i < NumSegments; i++)
            segments.Add(new Segment(this, i,i==NumSegments-1));
        CalculateCummulativeLengths();
        ///Calculate reference vectors
        List<PointOnCurve> points = GetSamplePoints();
        {
            Vector3 referenceVector = GetDefaultReferenceVector(points[0].tangent);
            referenceVector = referenceVector.normalized;
            points[0].reference = referenceVector;
            for (int i = 1; i < points.Count; i++)
            {
                var point = points[i];
                point.CalculateReference(points[i - 1], referenceVector);
                referenceVector =  point.reference.normalized;
                points[i].reference = referenceVector;
            }
        }
        if (isClosedLoop)
        {
            //angle difference between the final reference vector, and the first reference vector projected backwards
            Vector3 finalReferenceVector = points[points.Count - 1].reference;
            var point = points[points.Count - 1];
            point.CalculateReference(points[0], points[0].reference);
            Vector3 firstReferenceVectorProjectedBackwards = point.reference;
            float angleDifference = Vector3.SignedAngle(finalReferenceVector,firstReferenceVectorProjectedBackwards,points[points.Count-1].tangent);
            for (int i = 1; i < points.Count; i++)
                points[i].reference = Quaternion.AngleAxis((i/(float)(points.Count-1))*angleDifference,points[i].tangent) *points[i].reference;
        }
    }
    public List<PointOnCurve> GetPointsWithSpacing(float spacing)
    {
        var retr = new List<PointOnCurve>();
        float length = GetLength();
        int numInLength = Mathf.CeilToInt(length / spacing);
        int sampleReduction = isClosedLoop ?1:0;
        for (int i=0;i<=numInLength-sampleReduction;i++)
            retr.Add(GetPointAtDistance(i*length/numInLength));
        return retr;
    }

    public List<PointOnCurve> GetSamplePoints()
    {
        List<PointOnCurve> retr = new List<PointOnCurve>();
        foreach (var i in segments)
            foreach (var j in i.samples)
                retr.Add(j);
        return retr;
    }
    private void CalculateCummulativeLengths()
    {
        float cummulativeLength = 0;
        foreach (var i in segments)
        {
            foreach (var j in i.samples)
                j.distanceFromStartOfCurve = j.distanceFromStartOfSegment + cummulativeLength;//we add the cummulative length not including the current segment
            cummulativeLength += i.length;
            i.cummulativeLength = cummulativeLength;
        }
    }
    #endregion

    #region point manipulation
    public Vector3 this[int virtualIndex]
    {
        get
        {
            return GetPointGroupByIndex(virtualIndex).GetWorldPositionByIndex(GetPointTypeByIndex(virtualIndex));
        }
        set
        {
            GetPointGroupByIndex(virtualIndex).SetWorldPositionByIndex(GetPointTypeByIndex(virtualIndex),value);
        }
    }
    public Vector3 this[int segmentVirtualIndex,int pointVirtualIndex]
    {
        get
        {
            int index = GetVirtualIndex(segmentVirtualIndex, pointVirtualIndex);
            return this[index];
        }
        set
        {
            int index = GetVirtualIndex(segmentVirtualIndex,pointVirtualIndex);
            this[index] = value;
        }
    }
    #endregion

    public PGIndex GetPointTypeByIndex(int virtualIndex)
    {
        var length = PointGroups.Count * 3;
        if (virtualIndex == length)
            return PGIndex.Position;
        if (virtualIndex == length - 1)
            return PGIndex.LeftTangent;
        if (virtualIndex == length - 2)
            return PGIndex.RightTangent;
        int offsetIndex = virtualIndex - GetParentVirtualIndex(virtualIndex);
        return (PGIndex)offsetIndex;
    }
    public PGIndex GetOtherTangentIndex(PGIndex index)
    {
        switch (index)
        {
            case PGIndex.LeftTangent:
                return PGIndex.RightTangent;
            case PGIndex.RightTangent:
                return PGIndex.LeftTangent;
            case PGIndex.Position:
                return PGIndex.Position;
            default:
                throw new System.InvalidOperationException();
        }
    }
    public PointGroup GetPointGroupByIndex(int virtualIndex)
    {
        return PointGroups[GetPointGroupIndex(virtualIndex)];
    }

    public static int GetVirtualIndexByType(int parentPointGroupIndex,PGIndex type)
    {
        int retrVal = parentPointGroupIndex * 3;
        if (type== PGIndex.LeftTangent)
        {
            return retrVal - 1;
        } else if (type == PGIndex.RightTangent)
        {
            return retrVal+ 1;
        }
        return retrVal;
    }

    public int GetVirtualIndex(int segmentIndex,int pointIndex) { return segmentIndex * 3 + pointIndex; }
    public int GetParentVirtualIndex(int childVirtualIndex) { return GetPointGroupIndex(childVirtualIndex) * 3; }
    public int GetPointGroupIndex(int childIndex) {
        return ((childIndex + 1) / 3)%PointGroups.Count;
    }
}