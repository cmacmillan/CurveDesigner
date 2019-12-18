using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurveSplitPointInfo
{
    public int segmentIndex;
    public float time;
    public CurveSplitPointInfo(int segmentIndex, float time)
    {
        this.segmentIndex = segmentIndex;
        this.time = time;
    }
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
    public bool hasLeftTangent=false;
    [SerializeField]
    [HideInInspector]
    public bool hasRightTangent=false;
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
        this.hasLeftTangent = clone.hasLeftTangent;
        this.hasRightTangent = clone.hasRightTangent;
        this.isPointLocked = clone.isPointLocked;
        this.leftTangent = clone.leftTangent;
        this.rightTangent = clone.rightTangent;
        this.position = clone.position;
    }

    public bool DoesEditAffectBothSegments(PGIndex index)
    {
        switch (index)
        {
            case PGIndex.LeftTangent:
                return this.hasRightTangent;
            case PGIndex.RightTangent:
                return this.hasRightTangent;
            case PGIndex.Position:
            default:
                return true;
        }
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
        if (state == true && hasLeftTangent && hasRightTangent)
        {
            rightTangent = reflectAcrossPosition(leftTangent);
        }
    }
    public void SetWorldPositionByIndex(PGIndex index, Vector3 value)
    {
        switch (index)
        {
            case PGIndex.LeftTangent:
                hasLeftTangent = true;
                leftTangent = value - position;
                if (hasRightTangent && isPointLocked)
                {
                    rightTangent = reflectAcrossPosition(leftTangent);
                }
                return;
            case PGIndex.Position:
                position = value;
                return;
            case PGIndex.RightTangent:
                hasRightTangent = true;
                rightTangent = value - position;
                if (hasLeftTangent && isPointLocked)
                {
                    leftTangent = reflectAcrossPosition(rightTangent);
                }
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
                if (!hasLeftTangent)
                    throw new System.ArgumentException();
                return leftTangent + position;
            case PGIndex.Position:
                return position;
            case PGIndex.RightTangent:
                if (!hasRightTangent)
                    throw new System.ArgumentException();
                return rightTangent + position;
            default:
                throw new System.ArgumentException();
        }
    }
    #endregion

    public PointGroup(bool lockState)
    {
        hasLeftTangent = false;
        hasRightTangent = false;
        SetPointLocked(lockState);
    }
}
