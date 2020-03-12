using Assets.NewUI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Curve3D : MonoBehaviour
{
    [NonSerialized]
    public ClickHitData elementClickedDown;
    [NonSerialized]
    public CurveComposite UICurve=null;

    public bool drawNormals = true;
    public float normalsLength = 1.0f;

    public bool isClosedLoop = false;
    [SerializeField]
    [HideInInspector]
    private bool oldIsClosedLoop;

    public EditMode editMode=EditMode.PositionCurve;
    public BezierCurve positionCurve;
    public Texture2D lineTex;
    public Material testmat;

    public Texture2D blueLineTopTex;
    public Texture2D blueLineBottomTex;
    public Texture2D redLineTopTex;
    public Texture2D redLineBottomTex;
    public Color lineGray1;
    public Color lineGray2;

    public Texture2D circleIcon;
    public Texture2D squareIcon;
    public Texture2D diamondIcon;
    public Texture2D plusButton;
    public MeshFilter filter;
    public DateTime lastMeshUpdateStartTime;
    public DateTime lastMeshUpdateEndTime;
    public Mesh mesh;
    private bool isInitialized = false;

    [ContextMenu("Reset curve")]
    public void ResetCurve()
    {
        positionCurve.Initialize();
    }
    public void TryInitialize()
    {
        if (!isInitialized)
        {
            isInitialized = true;
            positionCurve.Initialize();
        }
    }

    [Header("Lock axis to position 0")]
    public bool lockXAxis;
    public bool lockYAxis;
    public bool lockZAxis;

    public FloatLinearValueDistanceSampler sizeDistanceSampler = new FloatLinearValueDistanceSampler();

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
    public float arcOfTube = 360;
    [SerializeField]
    [HideInInspector]
    private float oldArcOfTube = -1;

    [Range(0, 360)]

    public float curveRotation = 360;
    [SerializeField]
    [HideInInspector]
    private float oldCurveRotation= -1;

    [Min(0)]
    public float thickness = .1f;
    [HideInInspector]
    [SerializeField]
    private float oldTubeThickness = -1;

    public CurveType type = CurveType.HollowTube;
    [HideInInspector]
    [SerializeField]
    private CurveType oldType;

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
        retr|=CheckField(arcOfTube, ref oldArcOfTube);
        retr|=CheckField(curveRotation, ref oldCurveRotation);
        retr|=CheckField(thickness, ref oldTubeThickness);
        retr|=CheckField(type, ref oldType);
        if (CheckField(isClosedLoop, ref oldIsClosedLoop))
        {
            retr = true;
            positionCurve.Recalculate();
            UICurve.Initialize();
        }

        //retr|=CheckAnimationCurve(curveSizeAnimationCurve,ref _oldCurveSizeAnimationCurve);

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

    void Start()
    {
    }

    void Update()
    {
    }
    [ContextMenu("Test")]
    void Test()
    {
    BezierCurve2D.Test();
    }
}
public class KeyframeInfo
{
    public Keyframe frame;
    public int segmentIndex;
    public float progressAlongSegment;

    public int leftTangentIndex;
    public float leftTangentProgressAlongSegment;
    public float leftTangentValue;

    public int rightTangentIndex;
    public float rightTangentProgressAlongSegment;
    public float rightTangentValue;

    public KeyframeInfo(Keyframe frame, int segmentIndex, float progressAlongSegment)
    {
        this.frame = frame;
        this.segmentIndex = segmentIndex;
        this.progressAlongSegment = progressAlongSegment;
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
    HollowTube = 0,
    Flat = 1,
    DoubleBezier = 2,
    NoMesh = 3,
    Cylinder = 4,
}
