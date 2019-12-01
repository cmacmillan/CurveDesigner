using System.Collections;
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

    public void Initialize()
    {
        PointGroups = new List<PointGroup>();
        var pointA = new PointGroup();
        pointA.SetWorldPositionByIndex(PGIndex.Position, Vector3.zero);
        pointA.SetWorldPositionByIndex(PGIndex.RightTangent, new Vector3(1,0,0));
        PointGroups.Add(pointA);
        var pointB = new PointGroup();
        pointB.SetWorldPositionByIndex(PGIndex.Position, new Vector3(1,1,0));
        pointB.SetWorldPositionByIndex(PGIndex.LeftTangent, new Vector3(0,1,0));
        PointGroups.Add(pointB);
    }

    public void AddDefaultSegment()
    {
        var finalPointGroup = PointGroups[PointGroups.Count - 1];
        var finalPointPos = finalPointGroup.GetWorldPositionByIndex(PGIndex.Position);
        finalPointGroup.SetWorldPositionByIndex(PGIndex.RightTangent,finalPointPos+new Vector3(1,0,0));
        var pointB = new PointGroup();
        pointB.SetWorldPositionByIndex(PGIndex.Position,finalPointPos+new Vector3(1,1,0));
        pointB.SetWorldPositionByIndex(PGIndex.LeftTangent,finalPointPos+new Vector3(0,1,0));
        PointGroups.Add(pointB);
    }

    #region curve calculations
    public List<Vector3> SampleCurve(float sampleDistance)
    {
        List<Vector3> retr = new List<Vector3>();
        float lenSoFar = 0;
        for (int i = 0; i < NumSegments; i++)
        {
            float f = lenSoFar;
            float segmentLength = _lengths[i];
            lenSoFar += segmentLength;
            int numSteps = Mathf.Max(1, Mathf.RoundToInt(segmentLength / sampleDistance));
            float jumpDist = segmentLength / numSteps;
            for (int j = 0; j < numSteps; j++)
            {
                retr.Add(GetPositionAtDistance(f));
                f += jumpDist;
            }
        }
        retr.Add(GetPositionAtDistance(lenSoFar));//add last point
        return retr;
    }

    //Doesn't actually sample at distance along the beizer, but rather the position at distance/length, which isn't quite uniform

    public Vector3 GetPositionAtDistance(float distance)
    {
        for (int i=0;i<NumSegments;i++)
        {
            if (distance-_lengths[i]<0)
            {
                return GetSegmentPositionAtTime(i,distance/_lengths[i]);
            }
            else
            {
                distance -= _lengths[i];
            }
        }
        int finalSegmentIndex = NumSegments - 1;
        return GetSegmentPositionAtTime(finalSegmentIndex,1.0f);
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
        PointGroups[GetPhysicalIndex(index)].SetPointLocked(state);
    }
    public bool GetPointLockState(int index)
    {
        return PointGroups[GetPhysicalIndex(index)].GetIsPointLocked();
    }
    #endregion

    #region length calculation
    private List<float> _lengths;
    public float GetLength()
    {
        float len = 0;
        foreach (var i in _lengths)
        {
            len += i;
        }
        return len;
    }
    public void CacheLengths()
    {
        if (_lengths==null)
        {
            _lengths = new List<float>();
        } else
        {
            _lengths.Clear();
        }
        for (int i = 0; i < NumSegments; i++)
        {
            _lengths.Add(CalculateSegmentLength(i));
        }
    }
    private const int _numSegmentLengthSamples = 100;
    private float CalculateSegmentLength(int segmentIndex)
    {
        float len = 0;
        Vector3 previousPosition = GetSegmentPositionAtTime(segmentIndex,0.0f);
        for (int i = 1; i < _numSegmentLengthSamples; i++)
        {
            Vector3 currentPosition = GetSegmentPositionAtTime(segmentIndex, i / (float)_numSegmentLengthSamples);
            len += Vector3.Distance(currentPosition, previousPosition);
            previousPosition = currentPosition; 
        }
        return len;
    }
    #endregion

    #region point manipulation
    public Vector3 this[int virtualIndex]
    {
        get
        {
            int parentIndex = GetPhysicalIndex(virtualIndex);
            return PointGroups[parentIndex].GetWorldPositionByIndex(GetPointTypeByIndex(virtualIndex));
        }
        set
        {
            int offsetIndex = virtualIndex-GetParentVirtualIndex(virtualIndex);
            int parentIndex = GetPhysicalIndex(virtualIndex);
            PointGroups[parentIndex].SetWorldPositionByIndex(GetPointTypeByIndex(virtualIndex),value);
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
        int offsetIndex = virtualIndex-GetParentVirtualIndex(virtualIndex);
        return (PGIndex)offsetIndex;
    }

    private static int GetVirtualIndex(int segmentIndex,int pointIndex) { return segmentIndex * 3 + pointIndex; }
    private static int GetParentVirtualIndex(int childVirtualIndex) { return GetPhysicalIndex(childVirtualIndex) * 3; }
    private static int GetPhysicalIndex(int childIndex) { return ((childIndex + 1) / 3); }
}
