using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Curve3D : MonoBehaviour
{
    public CurveType type;
    public SelectedPointType editMode=SelectedPointType.PositionCurve;
    public BeizerCurve positionCurve;
    [Range(.01f,3)]
    public float sampleRate = 1.0f;
    public Texture2D lineTex;
    public Texture2D circleIcon;
    public Texture2D squareIcon;
    public Texture2D diamondIcon;

    public SelectedPointInfo selectedPoint=null;

    void Start()
    {
    }

    void Update()
    {
    }
}
[System.Serializable]
public class SelectedPointInfo
{
    public bool isPointHot;
    public SelectedPointType type;
    public int positionCurveIndex;
}
public enum SelectedPointType
{
    Unknown = 0,
    PositionCurve = 1,
    Rotation = 2,
}
public enum CurveType
{
    Tube = 0,
    Flat = 1,
    DoubleBeizer = 2,
}
