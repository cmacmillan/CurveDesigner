using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//A class which defines a chain of 3rd order beizer curves (4 control points per segment)
[System.Serializable]
public class BeizerCurve
{
    [HideInInspector]
    [SerializeField]
    private List<Vector3> points;

    [HideInInspector]
    [SerializeField]
    private List<bool> pointLocks;

    public int NumPoints { get { return points.Count; } }
    public int NumSegments { get { return points.Count / 3; } }
    public const int PointsPerSegment=4;

    #region curve calculations
    public List<Vector3> SampleCurve(float sampleDistance)
    {
        List<Vector3> retr = new List<Vector3>();
        float curveLength = GetLength();
        for (float f = 0; f < curveLength; f += sampleDistance)
        {
            retr.Add(GetPositionAtDistance(f));
        }
        return retr;
    }

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
        return GetSegmentPositionAtTime(finalSegmentIndex,distance/_lengths[finalSegmentIndex]);
    }
    public Vector3 GetSegmentPositionAtTime(int segmentIndex,float time)
    {
        return SolvePositionAtTime(GetPointIndex(segmentIndex,0),4,time);
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
        SetPointLockState(GetPointIndex(segmentIndex,pointIndex),state);
    }
    public bool GetPointLockState(int segmentIndex, int pointIndex)
    {
        return GetPointLockState(GetPointIndex(segmentIndex,pointIndex));
    }
    public void SetPointLockState(int index,bool state)
    {
        pointLocks[GetPointParentBaseIndex(index)] = state;
        if (state == true)
        {
            if (index > 0 && index < NumPoints - 1)
            {
                int parentIndex = GetPointParentPositionIndex(index);
                this[parentIndex - 1] = this[parentIndex - 1];//Calls the setter to ensure the mirroring
            }
        }
    }
    public bool GetPointLockState(int index)
    {
        return pointLocks[GetPointParentBaseIndex(index)];
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
    public Vector3 this[int index]
    {
        get
        {
            int parentIndex = GetPointParentPositionIndex(index);
            if (parentIndex == index)
            {
                return points[index];
            }
            else
            {
                return points[index] + points[parentIndex];
            }
        }
        set
        {
            int parentIndex = GetPointParentPositionIndex(index);
            if (parentIndex == index)
            {
                points[index] = value;
            }
            else
            {
                points[index] = value - points[parentIndex];
                if (pointLocks[GetPointParentBaseIndex(index)] == true && index > 0 && index < NumPoints - 1)
                {
                    int oppositePointIndex = -(index-parentIndex);
                    Vector3 vectorFromMiddlePointToPointA = points[index] - points[parentIndex];
                    points[oppositePointIndex] = points[parentIndex] - vectorFromMiddlePointToPointA;
                }
            }
        }
    }
    public Vector3 this[int segmentIndex,int pointIndex]
    {
        get
        {
            int index = GetPointIndex(segmentIndex, pointIndex);
            return this[index];
        }
        set
        {
            int index = GetPointIndex(segmentIndex,pointIndex);
            this[index] = value;
        }
    }
    #endregion

    private static int GetPointIndex(int segmentIndex,int pointIndex) { return segmentIndex * 3 + pointIndex; }
    private static int GetPointParentPositionIndex(int childIndex) { return GetPointParentBaseIndex(childIndex) * 3; }
    private static int GetPointParentBaseIndex(int childIndex) { return ((childIndex + 1) / 3); }
}
