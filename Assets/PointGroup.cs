using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurveSplitPointInfo : ISegmentTime
{
    public int segmentIndex;
    public float time;
    public CurveSplitPointInfo(int segmentIndex, float time)
    {
        this.segmentIndex = segmentIndex;
        this.time = time;
    }

    public int SegmentIndex { get { return segmentIndex; } }

    public float Time { get { return time; } }
}
public enum PGIndex
{
    LeftTangent = -1,
    Position = 0,
    RightTangent = 1
}
/// <summary>
/// A point group groups 3 control points together, left tangent, right tangent and the point itself
/// </summary>
[System.Serializable]
public class PointGroup
{
    #region fields
    [HideInInspector]
    [SerializeField]
    private bool isPointLocked=false;
    /// <summary>
    /// The right tangent in space relative to central point
    /// </summary>
    [HideInInspector]
    [SerializeField]
    private Vector3 leftTangent;
    /// <summary>
    /// The left tangent in space relative to central point
    /// </summary>
    [HideInInspector]
    [SerializeField]
    private Vector3 rightTangent;
    [HideInInspector]
    [SerializeField]
    private Vector3 position;
    #endregion

    public PointGroup() { }
    public PointGroup(PointGroup clone)
    {
        this.isPointLocked = clone.isPointLocked;
        this.leftTangent = clone.leftTangent;
        this.rightTangent = clone.rightTangent;
        this.position = clone.position;
    }

    public bool DoesEditAffectBothSegments(PGIndex index)
    {
        return true;
    }

    #region Get/Set methods
    public bool GetIsPointLocked()
    {
        return isPointLocked;
    }
    private Vector3 reflectAcrossPosition(Vector3 vect)
    {
        return -vect;
    }
    public void SetPointLocked(bool state)
    {
        isPointLocked = state;
        if (state == true)
        {
            rightTangent = reflectAcrossPosition(leftTangent);
        }
    }
    public void SetWorldPositionByIndex(PGIndex index, Vector3 value)
    {
        switch (index)
        {
            case PGIndex.LeftTangent:
                leftTangent = value - position;
                if (isPointLocked)
                    rightTangent = reflectAcrossPosition(leftTangent);
                return;
            case PGIndex.Position:
                position = value;
                return;
            case PGIndex.RightTangent:
                rightTangent = value - position;
                if (isPointLocked)
                    leftTangent = reflectAcrossPosition(rightTangent);
                return;
            default:
                throw new System.ArgumentException();
        }
    }
    public Vector3 GetWorldPositionByIndex(PGIndex index)
    {
        switch (index)
        {
            case PGIndex.LeftTangent:
                return leftTangent + position;
            case PGIndex.Position:
                return position;
            case PGIndex.RightTangent:
                return rightTangent + position;
            default:
                throw new System.ArgumentException();
        }
    }
    #endregion

    public PointGroup(bool lockState)
    {
        SetPointLocked(lockState);
    }
}
