using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Curve3D : MonoBehaviour
{
    public CurveType type;
    public EditMode editMode=EditMode.PositionCurve;
    public BeizerCurve positionCurve;
    public Texture2D lineTex;
    public Texture2D circleIcon;
    public Texture2D squareIcon;
    public Texture2D diamondIcon;
    public MeshFilter filter;
    public DateTime lastMeshUpdateStartTime;
    public DateTime lastMeshUpdateEndTime;
    public Mesh mesh;

    public bool IsAPointSelected
    {
        get
        {
            return hotPointIndex != -1;
        }
    }
    public List<int> selectedPointsIndex = new List<int>();
    public int hotPointIndex=-1;
    public Vector2 pointDragOffset;

    void Start()
    {
    }

    void Update()
    {
    }
}
public enum EditMode
{
    PositionCurve = 0,
    Rotation = 1,
}
public enum CurveType
{
    Tube = 0,
    Flat = 1,
    DoubleBeizer = 2,
}
