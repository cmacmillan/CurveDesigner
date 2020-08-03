﻿using Assets.NewUI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using static BezierCurve;

public class Curve3D : MonoBehaviour , ISerializationCallbackReceiver
{
    public IEnumerable<IDistanceSampler> DistanceSamplers
    {
        get
        {
            yield return sizeSampler;
            yield return rotationSampler;
            yield return doubleBezierSampler;
            yield return colorSampler;
        }
    }
    public IActiveElement ActiveElement
    {
        get
        {
            switch (editMode)
            {
                case EditMode.PositionCurve:
                    return positionCurve;
                case EditMode.Rotation:
                    return rotationSampler;
                case EditMode.Size:
                    return sizeSampler;
                case EditMode.DoubleBezier:
                    return doubleBezierSampler;
                case EditMode.Color:
                    return colorSampler;
                default:
                    throw new NotImplementedException();
            }
        }
    }

    public CollapsableCategory[] collapsableCategories =
    {
        new MainCollapsableCategory(),
        new TexturesCollapsableCategory(),
        new PreferencesCollapsableCategory(),
    };

    //sorted from most recent to oldest
    public List<SelectableGUID> selectedPoints = new List<SelectableGUID>();
    public ClickShiftControlState shiftControlState = ClickShiftControlState.none;

    public enum ClickShiftControlState
    {
        none=0,
        control=1,
        shift=2,
    }

    public void DeselectAllPoints() {
        selectedPoints.Clear();
    }
    public void SelectOnlyPoint(SelectableGUID point)
    {
        DeselectAllPoints();
        SelectAdditionalPoint(point);
    }
    public void SelectAdditionalPoint(SelectableGUID point)
    {
        if (!selectedPoints.Contains(point))
            selectedPoints.Insert(0, point);
    }
    public void DeselectPoint(SelectableGUID point)
    {
        selectedPoints.Remove(point);
    }
    public void ToggleSelectPoint(SelectableGUID point)
    {
        if (selectedPoints.Contains(point))
            DeselectPoint(point);
        else
            SelectAdditionalPoint(point);
    }

    [NonSerialized]
    public EditModeCategories editModeCategories = new EditModeCategories();

    //public Mesh testMesh;
    //public Material testMat;
    public CommandBuffer commandBuffer;

    public bool placeLockedPoints = true;
    public SplitInsertionNeighborModification splitInsertionBehaviour = SplitInsertionNeighborModification.DoNotModifyNeighbors;

    public SelectableGUIDFactory guidFactory = new SelectableGUIDFactory();

    [ContextMenu("ExportToObj")]
    public void ExportToObj()
    {
        ObjMeshExporter.DoExport(gameObject, false);
    }

    public void OnBeforeSerialize() { /* Do Nothing */ }

    public void OnAfterDeserialize()
    {
        foreach (var i in DistanceSamplers)
            i.RecalculateOpenCurveOnlyPoints(positionCurve);
    }

    public FloatDistanceSampler sizeSampler = new FloatDistanceSampler("Size",1);
    public FloatDistanceSampler rotationSampler = new FloatDistanceSampler("Rotation (degrees)",0);
    public ColorDistanceSampler colorSampler = new ColorDistanceSampler("Color");
    public DoubleBezierSampler doubleBezierSampler = new DoubleBezierSampler();

    public void RequestMeshUpdate()
    {
        lastMeshUpdateStartTime = DateTime.Now;
    }

    [HideInInspector]
    public float averageSize;
    [HideInInspector]
    public DateTime lastMeshUpdateStartTime;
    [HideInInspector]
    public DateTime lastMeshUpdateEndTime;
    [HideInInspector]
    public List<float> previousRotations = new List<float>();
    [HideInInspector]
    public BezierCurve positionCurve;

    [NonSerialized]
    public ClickHitData elementClickedDown;
    [NonSerialized]
    public UICurve UICurve=null;

    public bool showNormals = true;
    public bool showTangents = true;

    public EditMode editMode=EditMode.PositionCurve;

    public Curve3dSettings settings;

    public MeshFilter filter;
    [FormerlySerializedAs("mesh")]
    public Mesh displayMesh;
    private bool isInitialized = false;

    /// Start of properties that redraw the curve

    [SerializeField]
    [HideInInspector]
    private float old_constSize;

    [SerializeField]
    [HideInInspector]
    private float old_constRotation;

    [SerializeField]
    [HideInInspector]
    private Color old_constColor;

    [SerializeField]
    [HideInInspector]
    private InterpolationType old_sizeInterpolation;

    [SerializeField]
    [HideInInspector]
    private InterpolationType old_rotationInterpolation;

    [SerializeField]
    [HideInInspector]
    private InterpolationType old_colorInterpolation;

    public bool useSeperateInnerAndOuterFaceTextures;
    [SerializeField]
    [HideInInspector]
    private bool old_useSeperateInnerAndOuterFaceTextures;

    public DimensionLockMode lockToPositionZero;
    [SerializeField]
    [HideInInspector]
    private DimensionLockMode old_lockToPositionZero;

    [Min(0)]
    public float vertexDensity = 1.0f;
    [SerializeField]
    [HideInInspector]
    private float old_vertexDensity = -1;

    [Min(3)]
    public int ringPointCount = 8;
    [SerializeField]
    [HideInInspector]
    private int old_ringPointCount=-1;

    [Range(0, 360)]
    public float arcOfTube = 360;
    [SerializeField]
    [HideInInspector]
    private float old_arcOfTube = -1;

    [Min(0)]
    public float thickness = .1f;
    [HideInInspector]
    [SerializeField]
    private float old_thickness = -1;

    public CurveType type = CurveType.HollowTube;
    [HideInInspector]
    [SerializeField]
    private CurveType old_type;

    public float closeTilableMeshGap;
    [HideInInspector]
    [SerializeField]
    private float old_closeTilableMeshGap = -1;

    public Mesh meshToTile;
    [HideInInspector]
    [SerializeField]
    private Mesh old_meshToTile=null;

    public bool isClosedLoop = false;
    [SerializeField]
    [HideInInspector]
    private bool old_isClosedLoop;

    public MeshPrimaryAxis meshPrimaryAxis = MeshPrimaryAxis.auto;
    [SerializeField]
    [HideInInspector]
    private MeshPrimaryAxis old_meshPrimaryAxis;

    private bool CheckFieldChanged<T>(T field, ref T oldField)
    {
        if (!field.Equals(oldField))
        {
            oldField = field;
            return true;
        }
        return false;
    }

    public bool HaveCurveSettingsChanged()
    {
        bool retr = false;

        retr |= CheckFieldChanged(ringPointCount, ref old_ringPointCount);
        retr |= CheckFieldChanged(vertexDensity, ref old_vertexDensity);
        retr |= CheckFieldChanged(arcOfTube, ref old_arcOfTube);
        retr |= CheckFieldChanged(thickness, ref old_thickness);
        retr |= CheckFieldChanged(type, ref old_type);
        retr |= CheckFieldChanged(closeTilableMeshGap, ref old_closeTilableMeshGap);
        retr |= CheckFieldChanged(meshToTile, ref old_meshToTile);
        retr |= CheckFieldChanged(meshPrimaryAxis, ref old_meshPrimaryAxis);
        retr |= CheckFieldChanged(lockToPositionZero, ref old_lockToPositionZero);
        retr |= CheckFieldChanged(useSeperateInnerAndOuterFaceTextures, ref old_useSeperateInnerAndOuterFaceTextures);

        //color sampler
        retr |= CheckFieldChanged(colorSampler.constValue, ref old_constColor);
        retr |= CheckFieldChanged(colorSampler.Interpolation, ref old_colorInterpolation);

        //size sampler
        retr |= CheckFieldChanged(sizeSampler.constValue, ref old_constSize);
        retr |= CheckFieldChanged(sizeSampler.Interpolation, ref old_sizeInterpolation);

        //rotation sampler
        retr |= CheckFieldChanged(rotationSampler.constValue, ref old_constRotation);
        retr |= CheckFieldChanged(rotationSampler.Interpolation, ref old_rotationInterpolation);

        if (CheckFieldChanged(isClosedLoop, ref old_isClosedLoop))
        {
            retr = true;
            positionCurve.Recalculate();
            UICurve.Initialize();
        }
        return retr;
    }

    [ContextMenu("Reset curve")]
    public void ResetCurve()
    {
        positionCurve.Initialize();
        UICurve.Initialize();
    }
    [ContextMenu("Clear double")]
    public void ClearDouble()
    {
        doubleBezierSampler = new DoubleBezierSampler();
        this.UICurve.Initialize();
    }
    public void TryInitialize()
    {
        if (!isInitialized)
        {
            isInitialized = true;
            ResetCurve();
        }
    }
    public void CopyRotations()
    {
        previousRotations.Clear();
        foreach (var i in rotationSampler.GetPoints(this.positionCurve))
            previousRotations.Add(i.value);
    }
    public void CacheAverageSize()
    {
        float avg = 0;
        var points = sizeSampler.GetPoints(this.positionCurve);
        if (points.Count == 0)
        {
            averageSize = sizeSampler.constValue;
        }
        else
        {
            averageSize = 0;
            if (points.Count > 0)
            {
                foreach (var i in points)
                    avg += i.value;
                averageSize = avg / points.Count;
            }
        }
    }
    private const float _densityToDistanceDistanceMax = 100.0f;
    private float DensityToDistance(float density)
    {
        if (density <= 0.0f)
            return _densityToDistanceDistanceMax;
        return Mathf.Min(_densityToDistanceDistanceMax, 10.0f / density);
    }
    public float GetVertexDensityDistance() { return DensityToDistance(vertexDensity);}
    private const float normalValueLengthDivisor = 2.0f;
    private const float normalGapSizeMultiplier = 2.0f;
    public float VisualNormalsLength()
    {
        return averageSize/normalValueLengthDivisor;
    }
    public float GetNormalDensityDistance() { return VisualNormalsLength()*normalGapSizeMultiplier; }
}
public class EditModeCategories
{
    public Dictionary<EditMode, string> editmodeNameMap = new Dictionary<EditMode, string>()
        {
            {EditMode.PositionCurve, "Position"},
            {EditMode.Size, "Size"},
            {EditMode.Rotation, "Rotation"},
            {EditMode.DoubleBezier, "Double Bezier"},
            {EditMode.Color, "Color" },
        };
    public EditMode[] editModes;
    public GUIStyle _centeredStyle;
    private GUIStyle CenteredStyle
    {
        get
        {
            if (_centeredStyle == null)
            {
                _centeredStyle = GUI.skin.GetStyle("Label");
                _centeredStyle.alignment = TextAnchor.UpperCenter;
            }
            return _centeredStyle;
        }
    }
    public EditModeCategories()
    {
        var baseEditModes = System.Enum.GetValues(typeof(EditMode));
        var baseEditModeNames = System.Enum.GetNames(typeof(EditMode));
        editModes = new EditMode[baseEditModes.Length];
        for (int i = 0; i < editModes.Length; i++)
            editModes[i] = (EditMode)baseEditModes.GetValue(i);
    }
}
public enum InterpolationType
{
    Constant=0,
    Keyframes=1
}
public enum EditMode
{
    PositionCurve = 0,
    Rotation = 1,
    Size = 2,
    DoubleBezier = 3,
    Color = 4,
}
public enum CurveType
{
    HollowTube = 0,
    Flat = 1,
    DoubleBezier = 2,
    NoMesh = 3,
    Cylinder = 4,
    Mesh = 5,
}
