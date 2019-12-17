using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Curve3D : MonoBehaviour
{
    public CurveType type;
    public EditMode editMode=EditMode.PositionCurve;
    public BeizerCurve positionCurve;
    public Texture2D lineTex;
    public Texture2D circleIcon;
    public Texture2D squareIcon;
    public Texture2D diamondIcon;
    public Texture2D plusButton;
    public MeshFilter filter;
    public DateTime lastMeshUpdateStartTime;
    public DateTime lastMeshUpdateEndTime;
    public Mesh mesh;

    [Header("Lock axis to position 0")]
    public bool lockXAxis;
    public bool lockYAxis;
    public bool lockZAxis;


    public AnimationCurve curveSizeAnimationCurve;
    [SerializeField]
    [HideInInspector]
    private AnimationCurve _oldCurveSizeAnimationCurve = new AnimationCurve();

    public static AnimationCurve CopyAnimationCurve(AnimationCurve curve)
    {
        return new AnimationCurve(curve.keys);
    }
    public static bool DoKeyframesMatch(Keyframe k1, Keyframe k2)
    {
        if (k1.inTangent != k2.inTangent)
            return false;
        if (k1.inWeight != k2.inWeight)
            return false;
        if (k1.outTangent != k2.outTangent)
            return false;
        if (k1.outWeight != k2.outWeight)
            return false;
        if (k1.time != k2.time)
            return false;
        if (k1.value != k2.value)
            return false;
        if (k1.weightedMode != k2.weightedMode)
            return false;
        return true;
    }
    public static bool DoAnimationCurvesMatch(AnimationCurve curve1,AnimationCurve curve2)
    {
        if (curve1.length != curve2.length)
            return false;
        if (curve1.postWrapMode != curve2.postWrapMode)
            return false;
        if (curve1.preWrapMode != curve2.preWrapMode)
            return false;
        for (int i = 0; i < curve1.keys.Length; i++)
        {
            if (!DoKeyframesMatch(curve1.keys[i], curve2.keys[i]))
                return false;
        }
        return true;
    }
    //public List<FloatFieldKeyframe> curveSize;


    [Min(.001f)]
    public float curveVertexDensity=1.0f;
    [SerializeField]
    [HideInInspector]
    private float oldCurveVertexDensity=-1;

    [Min(0)]
    public float curveRadius =3.0f;
    [SerializeField]
    [HideInInspector]
    private float oldCurveRadius=-1;

    [Min(3)]
    public int ringPointCount = 8;
    [SerializeField]
    [HideInInspector]
    private int oldRingPointCount=-1;

    [Range(0, 360)]
    public int angleOfTube = 360;
    [SerializeField]
    [HideInInspector]
    private int oldAngleOfTube= -1;

    [Range(0, 360)]
    public int curveRotation = 360;
    [SerializeField]
    [HideInInspector]
    private int oldCurveRotation= -1;

    public bool HaveCurveSettingsChanged()
    {
        bool CheckField<T>(T field, ref T oldField)
        {
            if (!field.Equals(oldField))
            {
                oldField = field;
                return true;
            }
            return false;
        }
        bool CheckAnimationCurve(AnimationCurve newCurve, ref AnimationCurve oldCurve)
        {
            if (!DoAnimationCurvesMatch(newCurve,oldCurve))
            {
                oldCurve = CopyAnimationCurve(newCurve);
                return true;  
            }
            return false;
        }
        bool retr = false;
        retr|=CheckField(ringPointCount, ref oldRingPointCount);
        retr|=CheckField(curveVertexDensity, ref oldCurveVertexDensity);
        retr|=CheckField(curveRadius, ref oldCurveRadius);
        retr|=CheckField(angleOfTube, ref oldAngleOfTube);
        retr|=CheckField(curveRotation, ref oldCurveRotation);
        retr |= CheckAnimationCurve(curveSizeAnimationCurve,ref _oldCurveSizeAnimationCurve);
        return retr;
    }

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
    Size = 2,
}
public enum CurveType
{
    Tube = 0,
    Flat = 1,
    DoubleBeizer = 2,
}
