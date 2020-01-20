﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//A class which defines a chain of 3rd order beizer curves (4 control points per segment)
[System.Serializable]
public class BeizerCurve
{
    [SerializeField]
    [HideInInspector]
    public List<PointGroup> PointGroups;
    public int NumControlPoints { get { return PointGroups.Count*3-2; } }
    public int NumSegments { get { return PointGroups.Count-1; } }
    public bool placeLockedPoints = true;
    public bool isCurveOutOfDate = true;
    public SplitInsertionNeighborModification splitInsertionBehaviour;
    [SerializeField]
    [HideInInspector]
    public List<Segment> segments = null;
    
    public BeizerCurve() { }
    public BeizerCurve(BeizerCurve curveToClone)
    {
        PointGroups = new List<PointGroup>();
        foreach (var i in curveToClone.PointGroups)
        {
            PointGroups.Add(new PointGroup(i));
        }
        this.segments = new List<Segment>(curveToClone.segments);
        throw new System.NotImplementedException();//need to clone segments
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
        pointA.SetWorldPositionByIndex(PGIndex.RightTangent, new Vector3(1,0,0));
        PointGroups.Add(pointA);
        var pointB = new PointGroup(placeLockedPoints);
        pointB.SetWorldPositionByIndex(PGIndex.Position, new Vector3(1,1,0));
        pointB.SetWorldPositionByIndex(PGIndex.LeftTangent, new Vector3(0,1,0));
        PointGroups.Add(pointB);
    }

    public int InsertSegmentAfterIndex(CurveSplitPointInfo splitPoint,bool lockPlacedPoint,SplitInsertionNeighborModification shouldModifyNeighbors)
    {
        var prePointGroup = PointGroups[splitPoint.segmentIndex];
        var postPointGroup = PointGroups[splitPoint.segmentIndex + 1];
        PointGroup point = new PointGroup(lockPlacedPoint);
        var basePosition = this.GetSegmentPositionAtTime(splitPoint.segmentIndex, splitPoint.time);
        point.SetWorldPositionByIndex(PGIndex.Position,basePosition);
        Vector3 leftTangent;
        Vector3 rightTangent;
        Vector3 preLeftTangent;
        Vector3 postRightTangent;
        SolvePositionAtTimeTangents(GetVirtualIndex(splitPoint.segmentIndex, 0), 4, splitPoint.time, out leftTangent, out rightTangent, out preLeftTangent, out postRightTangent);

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

        PointGroups.Insert(splitPoint.segmentIndex+1,point);
        return (splitPoint.segmentIndex+1);
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

    public PointOnCurve GetPositionAtDistance(float distance)
    {
        float remainingDistance= distance;
        float time;
        Vector3 position;
        for (int i=0;i<NumSegments;i++)
        {
            if (remainingDistance < segments[i].length)
            {
                time = segments[i].GetTimeAtLength(remainingDistance);
                position = GetSegmentPositionAtTime(i, time);
                return new PointOnCurve(time,remainingDistance,position,distance,i);
            }
            remainingDistance-= segments[i].length;
        }
        int finalSegmentIndex = NumSegments - 1;
        time = 1.0f;
        position = GetSegmentPositionAtTime(finalSegmentIndex,time);
        return new PointOnCurve(time,segments[finalSegmentIndex].length,position,GetLength(),finalSegmentIndex);
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
    /// <summary>
    /// must call after modifying points
    /// </summary>
    public void Recalculate()
    {
        if (segments==null)
            segments = new List<Segment>();
        else
            segments.Clear();
        for (int i = 0; i < NumSegments; i++)
        {
            segments.Add(new Segment(this,i));
        }
        CalculateCummulativeLengths();
    }
    public List<PointOnCurve> GetPoints()
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

    public static PGIndex GetPointTypeByIndex(int virtualIndex)
    {
        int offsetIndex = virtualIndex-GetParentVirtualIndex(virtualIndex);
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

    public static int GetVirtualIndex(int segmentIndex,int pointIndex) { return segmentIndex * 3 + pointIndex; }
    public static int GetParentVirtualIndex(int childVirtualIndex) { return GetPointGroupIndex(childVirtualIndex) * 3; }
    public static int GetPointGroupIndex(int childIndex) { return ((childIndex + 1) / 3); }
}
