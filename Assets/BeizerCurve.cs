using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//A class which defines a chain of 3rd order beizer curves (4 control points per segment)
[System.Serializable]
public partial class BeizerCurve
{
    [SerializeField]
    [HideInInspector]
    public List<PointGroup> PointGroups;
    public int NumControlPoints { get { return owner.isClosedLoop?PointGroups.Count*3:PointGroups.Count*3-2; } }
    public int NumSegments { get { return owner.isClosedLoop?PointGroups.Count:PointGroups.Count-1; } }
    public bool placeLockedPoints = true;
    public bool isCurveOutOfDate = true;
    public SplitInsertionNeighborModification splitInsertionBehaviour;
    public Curve3D owner;
    [System.NonSerialized]
    [HideInInspector]
    public List<Segment> segments = null;

    private Vector3 GetTangent(int index)
    {
        return (this[index + 1] - this[index]);//.normalized;
    }
    
    public BeizerCurve() { }
    public BeizerCurve(BeizerCurve curveToClone)
    {
        PointGroups = new List<PointGroup>();
        foreach (var i in curveToClone.PointGroups)
        {
            PointGroups.Add(new PointGroup(i));
        }
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

    /// <summary>
    /// points produced by this method don't have correct reference vectors
    /// </summary>
    /// <param name="distance"></param>
    /// <returns></returns>
    public PointOnCurve GetPointAtDistance(float distance)
    {
        float remainingDistance= distance;
        for (int i=0;i<NumSegments;i++)
        {
            if (remainingDistance < segments[i].length)
            {
                float time = segments[i].GetTimeAtLength(remainingDistance);
                Vector3 position = GetSegmentPositionAtTime(i, time);
                Vector3 tangent = GetSegmentTangentAtTime(i, time);
                var retr = new PointOnCurve(time,remainingDistance,position,i,tangent);
                retr.distanceFromStartOfCurve = retr.distanceFromStartOfSegment + (i - 1 >= 0 ? segments[i - 1].cummulativeLength : 0);
                return retr;
            }
            remainingDistance-= segments[i].length;
        }
        {
            int finalSegmentIndex = NumSegments - 1;
            float time = 1.0f;
            Vector3 position = GetSegmentPositionAtTime(finalSegmentIndex, time);
            Vector3 tangent = GetSegmentTangentAtTime(finalSegmentIndex, time);
            var retr = new PointOnCurve(time, segments[finalSegmentIndex].length, position, finalSegmentIndex,tangent);
            retr.distanceFromStartOfCurve = retr.distanceFromStartOfSegment + (finalSegmentIndex - 1 >= 0 ? segments[finalSegmentIndex - 1].cummulativeLength : 0);
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
        return SolveTangentAtTime(GetVirtualIndex(segmentIndex,0),3,time);
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
        //Double reflection method for calculating RMF
        Vector3 DoubleReflectionRMF(Vector3 x0,Vector3 x1,Vector3 t0,Vector3 t1,Vector3 r0)
        {
            Vector3 v1 = x1 - x0;
            float c1 = Vector3.Dot(v1, v1);
            Vector3 rL = r0- (2.0f / c1) * Vector3.Dot(v1, r0) * v1;
            Vector3 tL = t0 - (2.0f / c1) * Vector3.Dot(v1, t0) * v1;
            Vector3 v2 = t1 - tL;
            float c2 = Vector3.Dot(v2, v2);
            return rL - (2.0f / c2) * Vector3.Dot(v2, rL) * v2;
        }
        Vector3 GetReference(PointOnCurve currentPoint, PointOnCurve previousPoint, Vector3 previousReference)
        {
            return DoubleReflectionRMF(previousPoint.position,currentPoint.position,previousPoint.tangent.normalized,currentPoint.tangent.normalized,previousReference);
        }
        List<PointOnCurve> points = GetPoints();
        {
            Vector3 referenceVector = NormalTangent(points[0].tangent, Vector3.up);
            referenceVector = referenceVector.normalized;
            points[0].reference = referenceVector;
            for (int i = 1; i < points.Count; i++)
            {
                referenceVector = GetReference(points[i], points[i - 1], referenceVector).normalized;
                points[i].reference = referenceVector;
            }
        }
        if (owner.isClosedLoop)
        {
            //angle difference between the final reference vector, and the first reference vector projected backwards
            Vector3 finalReferenceVector = points[points.Count - 1].reference;
            Vector3 firstReferenceVectorProjectedBackwards = GetReference(points[points.Count-1],points[0],points[0].reference);
            float angleDifference = Vector3.SignedAngle(finalReferenceVector,firstReferenceVectorProjectedBackwards,points[points.Count-1].tangent);
            for (int i = 1; i < points.Count; i++)
                points[i].reference = Quaternion.AngleAxis((i/(float)(points.Count-1))*angleDifference,points[i].tangent) *points[i].reference;
        }
    }
    public List<PointOnCurve> GetPoints()
    {
        List<PointOnCurve> retr = new List<PointOnCurve>();
        bool isFirstSegment = true;//We only want to add the first point of the first segment, every other segment should omit the first point
        foreach (var i in segments)
        {
            int c = 0;
            foreach (var j in i.samples)
            {
                if (c > 0 || isFirstSegment)
                {
                    retr.Add(j);
                    isFirstSegment = false;
                }
                c++;
            }
        }
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
