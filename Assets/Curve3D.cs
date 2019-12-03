using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Curve3D : MonoBehaviour
{
    public CurveType type;
    public EditMode editMode=EditMode.PositionCurve;
    public BeizerCurve positionCurve;
    [Range(.01f,3)]
    public float sampleRate = 1.0f;
    public Texture2D lineTex;
    public Texture2D circleIcon;
    public Texture2D squareIcon;
    public Texture2D diamondIcon;

    public bool IsAPointSelected
    {
        get
        {
            return hotPointIndex != -1;
        }
    }
    public int recentlySelectedPointIndex=-1;
    public int hotPointIndex=-1;
    public Vector2 pointDragOffset;
    public bool arePointsLocked = false;

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
