using Assets.NewUI;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
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
public enum DimensionLockMode
{
    none,
    x,
    y,
    z
}
/// <summary>
/// A point group groups 3 control points together, left tangent, right tangent and the point itself
/// </summary>
[System.Serializable]
public class PointGroup : ISelectable<PointGroup>
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
    [HideInInspector]
    [SerializeField]
    private SelectableGUID guid;

    public SelectableGUID GUID => guid;
    #endregion
    public PointGroup(bool lockState,Curve3D curve)
    {
        SetPointLocked(lockState);
        guid = curve.guidFactory.GetGUID();
    }
    public PointGroup(PointGroup clone,Curve3D curve)
    {
        this.isPointLocked = clone.isPointLocked;
        this.leftTangent = clone.leftTangent;
        this.rightTangent = clone.rightTangent;
        this.position = clone.position;
        //TODO:  //this.guid = curve.guidFactory.GetGUID();//commenting this for now, but it just means guids will be broken when position curves get cloned
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 LockAxis(Vector3 vector, DimensionLockMode lockMode)
    {
        switch (lockMode)
        {
            case DimensionLockMode.none:
                return vector;
            case DimensionLockMode.x:
                return new Vector3(0,vector.y,vector.z);
            case DimensionLockMode.y:
                return new Vector3(vector.x,0,vector.z);
            case DimensionLockMode.z:
                return new Vector3(vector.x,vector.y,0);
            default:
                return Vector3.zero;
        }
    }
    public void SetWorldPositionByIndex(PGIndex index, Vector3 value, DimensionLockMode dimensionLockMode)
    {
        switch (index)
        {
            case PGIndex.LeftTangent:
                leftTangent = LockAxis(value,dimensionLockMode) - GetWorldPositionByIndex(PGIndex.Position,dimensionLockMode);
                if (isPointLocked)
                    rightTangent = reflectAcrossPosition(leftTangent);
                return;
            case PGIndex.Position:
                position = LockAxis(value, dimensionLockMode);
                return;
            case PGIndex.RightTangent:
                rightTangent = LockAxis(value, dimensionLockMode) - GetWorldPositionByIndex(PGIndex.Position,dimensionLockMode);
                if (isPointLocked)
                    leftTangent = reflectAcrossPosition(rightTangent);
                return;
            default:
                throw new System.ArgumentException();
        }
    }
    public Vector3 GetWorldPositionByIndex(PGIndex index, DimensionLockMode dimensionLockMode)
    {
        switch (index)
        {
            case PGIndex.LeftTangent:
                return LockAxis(leftTangent + position,dimensionLockMode);
            case PGIndex.Position:
                return LockAxis(position,dimensionLockMode);
            case PGIndex.RightTangent:
                return LockAxis(rightTangent + position, dimensionLockMode);
            default:
                throw new System.ArgumentException();
        }
    }

    public float DistanceAlongCurve(BezierCurve positionCurve)
    {
        return positionCurve.GetDistanceAtSegmentIndexAndTime(positionCurve.PointGroups.IndexOf(this), 0);
    }

    public bool SelectEdit(Curve3D curve,List<PointGroup> selectedPoints)
    {
        var initialLocked = isPointLocked;
        bool? isLocked = null;
        bool currentLockState = EditorGUILayout.Toggle("Tangents Locked", initialLocked);//, "ToggleMixed");
        if (initialLocked != currentLockState)
            isLocked = currentLockState;

        var initialLeft = GetWorldPositionByIndex(PGIndex.LeftTangent, curve.lockToPositionZero);
        var leftTangent = EditorGUILayout.Vector3Field("Left Tangent", initialLeft)-initialLeft;

        var initialPos = GetWorldPositionByIndex(PGIndex.Position, curve.lockToPositionZero);
        var worldPos = EditorGUILayout.Vector3Field("Position", initialPos)-initialPos;

        var initialRight = GetWorldPositionByIndex(PGIndex.RightTangent, curve.lockToPositionZero);
        var rightTangent = EditorGUILayout.Vector3Field("Right Tangent", initialRight)-initialRight;

        if (isLocked==initialLocked && initialLeft==leftTangent && initialPos==worldPos && initialRight == rightTangent)
        {
            offsetMod = null;
            return false;
        }
        offsetMod = new PointGroupOffsetModification(isLocked, worldPos, leftTangent, rightTangent);
        return true;
    }
    #endregion
}
