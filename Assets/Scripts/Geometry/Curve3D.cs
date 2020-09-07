using Assets.NewUI;
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
            yield return arcOfTubeSampler;
            yield return thicknessSampler;
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
                case EditMode.Thickness:
                    return thicknessSampler;
                case EditMode.Arc:
                    return arcOfTubeSampler;
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

    public Mesh graphicsMesh;
    public Material graphicsMaterial;
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

    #region guistyles
    GUIStyle GetStyle(ref GUIStyle style,string init) {
        if (style == null)
            style = init;
        return style;
    }

    private GUIStyle _centeredStyle;
    public GUIStyle CenteredStyle
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
    //////////////////////////////////////////////////////////////////////////////
    private GUIStyle m_buttonStyle;
    public GUIStyle buttonStyle => GetStyle(ref m_buttonStyle,"Button");
    //////////////////////////////////////////////////////////////////////////////
    private GUIStyle m_particleLabelStyle;
    public GUIStyle particleLabelStyle => GetStyle(ref m_particleLabelStyle,"ShurikenLabel");
    //////////////////////////////////////////////////////////////////////////////
    private GUIStyle m_controlRectStyle;
    public GUIStyle controlRectStyle { get { if (m_controlRectStyle == null) { m_controlRectStyle = new GUIStyle { margin = new RectOffset(0, 0, 2, 2) };} return m_controlRectStyle; } } 
    //////////////////////////////////////////////////////////////////////////////
    private GUIStyle m_effectBgStyle;

    public GUIStyle effectBgStyle => GetStyle(ref m_effectBgStyle,"ShurikenEffectBg");
    //////////////////////////////////////////////////////////////////////////////
    private GUIStyle m_shurikenModuleBg;
    public GUIStyle  shurikenModuleBg=> GetStyle(ref m_shurikenModuleBg,"ShurikenModuleBg");
    //////////////////////////////////////////////////////////////////////////////
    private GUIStyle m_mixedToggleStyle;
    public GUIStyle  mixedToggleStyle =>GetStyle(ref m_mixedToggleStyle,"ShurikenToggleMixed");
    //////////////////////////////////////////////////////////////////////////////
    private GUIStyle m_initialHeaderStyle;
    public GUIStyle  initialHeaderStyle=> GetStyle(ref m_initialHeaderStyle,"ShurikenEmitterTitle");
    //////////////////////////////////////////////////////////////////////////////
    private GUIStyle m_nonInitialHeaderStyle;
    public GUIStyle  nonInitialHeaderStyle=> GetStyle(ref m_nonInitialHeaderStyle,"ShurikenModuleTitle");
    //////////////////////////////////////////////////////////////////////////////
    private GUIStyle m_dropdownStyle;
    public GUIStyle dropdownStyle=> GetStyle(ref m_dropdownStyle,"ShurikenDropdown");
    #endregion

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
        serializedObj = null;
        fields.Clear();
    }

    public FloatDistanceSampler sizeSampler = new FloatDistanceSampler("Size",1,EditMode.Size);
    public FloatDistanceSampler arcOfTubeSampler = new FloatDistanceSampler("Arc", 180,EditMode.Arc);
    public FloatDistanceSampler thicknessSampler = new FloatDistanceSampler("Thickness", .1f,EditMode.Thickness);
    public FloatDistanceSampler rotationSampler = new FloatDistanceSampler("Rotation",0,EditMode.Rotation);
    public ColorDistanceSampler colorSampler = new ColorDistanceSampler("Color",EditMode.Color);
    public DoubleBezierSampler doubleBezierSampler = new DoubleBezierSampler("Double Bezier",EditMode.DoubleBezier);

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
    public void BindDataToPositionCurve()
    {
        positionCurve.owner = this;
        positionCurve.isClosedLoop = isClosedLoop;
        positionCurve.dimensionLockMode = lockToPositionZero;
    }

    #region serializedObj
    [NonSerialized]
    private SerializedObject serializedObj;
    [NonSerialized]
    private Dictionary<string, SerializedProperty> fields = new Dictionary<string, SerializedProperty>();
    public void Field(string fieldName)
    {
        if (serializedObj == null)
            serializedObj= new SerializedObject(this);
        EditorGUILayout.PropertyField(GetField(fieldName));
    }
    private SerializedProperty GetField(string fieldName)
    {
        if (!fields.ContainsKey(fieldName))
            fields.Add(fieldName, serializedObj.FindProperty(fieldName));
        return fields[fieldName];
    }
    public void ApplyFieldChanges()
    {
        if (serializedObj != null)
            serializedObj.ApplyModifiedProperties();
    }
    /// Shuriken field with dropdown triangle
    protected const float k_minMaxToggleWidth = 13;
    protected static Rect GetPopupRect(Rect position)
    {
        position.xMin = position.xMax - k_minMaxToggleWidth;
        return position;
    }
    protected static Rect SubtractPopupWidth(Rect position)
    {
        position.width -= 1 + k_minMaxToggleWidth;
        return position;
    }

    private const int kSingleLineHeight = 18;

    protected static Rect GetControlRect(int height, Curve3D curve, params GUILayoutOption[] layoutOptions)
    {
        return GUILayoutUtility.GetRect(0, height, curve.controlRectStyle, layoutOptions);
    }

    public void EditModeSwitchButton(string label, EditMode mode,Rect rect)
    {
        EditMode thisEditMode = mode;
        bool isSelected = editMode == thisEditMode;
        GUI.Label(new Rect(rect.position, new Vector2(EditorGUIUtility.labelWidth, rect.height)), label, EditorStyles.label);
        rect.xMin += EditorGUIUtility.labelWidth;
        if (GUI.Toggle(rect, isSelected, EditorGUIUtility.TrTextContent($"{(isSelected ? "Editing" : "Edit")} {label}"), buttonStyle))
            editMode = thisEditMode;
    }
    public Rect GetFieldRects(out Rect popupRect)
    {
        Rect rect = GetControlRect(kSingleLineHeight, this);
        popupRect = GetPopupRect(rect);
        popupRect.height = kSingleLineHeight;
        rect = SubtractPopupWidth(rect);
        return rect;
    }
    public void SamplerField(string path, IValueSampler sampler)
    {
        if (serializedObj == null)
            serializedObj= new SerializedObject(this);
        Rect rect = GetFieldRects(out Rect popupRect);

        ValueType state = sampler.ValueType;

        switch (state)
        {
            case ValueType.Constant:
                EditorGUI.PropertyField(rect, GetField($"{path}.constValue"), new GUIContent(sampler.GetLabel()));
                break;
            case ValueType.Keyframes:
                EditModeSwitchButton(sampler.GetLabel(), sampler.GetEditMode(), rect);
                break;
        }

        // PopUp minmaxState menu
        if (EditorGUI.DropdownButton(popupRect, GUIContent.none, FocusType.Passive, dropdownStyle))
        {
            GUIContent[] texts =        {   EditorGUIUtility.TrTextContent("Constant"),
                                                EditorGUIUtility.TrTextContent("Curve") };
            ValueType[] states = {  ValueType.Constant,
                                        ValueType.Keyframes};
            GenericMenu menu = new GenericMenu();
            for (int i = 0; i < texts.Length; ++i)
            {
                menu.AddItem(texts[i], state == states[i], SelectValueTypeState, new SelectValueTypeStateTuple(sampler, states[i], this));
            }
            menu.DropDown(popupRect);
            Event.current.Use();
        }
    }

    private class SelectValueTypeStateTuple
    {
        public IValueSampler sampler;
        public ValueType mode;
        public Curve3D curve;
        public SelectValueTypeStateTuple(IValueSampler sampler, ValueType mode, Curve3D curve)
        {
            this.sampler = sampler;
            this.mode = mode;
            this.curve = curve;
        }
    }
    void SelectValueTypeState(object arg)
    {
        var tuple = arg as SelectValueTypeStateTuple;
        if (tuple != null)
        {
            tuple.sampler.ValueType = tuple.mode;
            if (tuple.mode == ValueType.Constant && tuple.curve.editMode == tuple.sampler.GetEditMode())
            {
                tuple.curve.editMode = EditMode.PositionCurve;//default to position
            }
            if (tuple.mode == ValueType.Keyframes)
                tuple.curve.editMode = tuple.sampler.GetEditMode();
            HandleUtility.Repaint();
        }
    }
    #endregion

    [NonSerialized]
    public ClickHitData elementClickedDown;
    [NonSerialized]
    public UICurve UICurve = null;

    public bool showPositionHandles = false;
    public bool showPointSelectionWindow = true;
    public bool showNormals = true;
    public bool showTangents = true;

    public EditMode editMode = EditMode.PositionCurve;

    public Curve3dSettings settings;

    public MeshCollider collider;
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
    private float old_constArcOfTube;

    [SerializeField]
    [HideInInspector]
    private float old_constThickness;

    [SerializeField]
    [HideInInspector]
    private ValueType old_sizeInterpolation;

    [SerializeField]
    [HideInInspector]
    private ValueType old_rotationInterpolation;

    [SerializeField]
    [HideInInspector]
    private ValueType old_colorInterpolation;

    [SerializeField]
    [HideInInspector]
    private ValueType old_arcOfTubeInterpolation;

    [SerializeField]
    [HideInInspector]
    private ValueType old_thicknessInterpolation;

    public bool clampAndStretchMeshToCurve = true;
    [SerializeField]
    [HideInInspector]
    private bool old_clampAndStretchMeshToCurve;

    public bool seperateInnerOuterTextures;
    [SerializeField]
    [HideInInspector]
    private bool old_seperateInnerOuterTextures;

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

    /*
    public Texture2D displacementTexture = null;
    [SerializeField]
    [HideInInspector]
    private Texture2D old_displacementTexture=null;
    [SerializeField]
    [HideInInspector]
    public Color32[] displacementTextureColors = null;
    */

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

        void CheckSamplerChanged<T>(IValueSampler<T> sampler,ref T oldConst, ref ValueType oldInterpolation)
        {
            retr |= CheckFieldChanged(sampler.ConstValue, ref oldConst);
            retr |= CheckFieldChanged(sampler.ValueType, ref oldInterpolation);
        }
        retr |= CheckFieldChanged(ringPointCount, ref old_ringPointCount);
        retr |= CheckFieldChanged(vertexDensity, ref old_vertexDensity);
        retr |= CheckFieldChanged(type, ref old_type);
        retr |= CheckFieldChanged(closeTilableMeshGap, ref old_closeTilableMeshGap);
        retr |= CheckFieldChanged(meshToTile, ref old_meshToTile);
        retr |= CheckFieldChanged(meshPrimaryAxis, ref old_meshPrimaryAxis);
        retr |= CheckFieldChanged(lockToPositionZero, ref old_lockToPositionZero);
        retr |= CheckFieldChanged(seperateInnerOuterTextures, ref old_seperateInnerOuterTextures);
        retr |= CheckFieldChanged(clampAndStretchMeshToCurve, ref old_clampAndStretchMeshToCurve);

        CheckSamplerChanged(colorSampler, ref old_constColor, ref old_colorInterpolation);
        CheckSamplerChanged(sizeSampler, ref old_constSize, ref old_sizeInterpolation);
        CheckSamplerChanged(rotationSampler, ref old_constRotation, ref old_rotationInterpolation);
        CheckSamplerChanged(arcOfTubeSampler, ref old_constArcOfTube, ref old_arcOfTubeInterpolation);
        CheckSamplerChanged(thicknessSampler, ref old_constThickness, ref old_thicknessInterpolation);

        retr |= CheckClosedLoopToggled();
        /*
        if (displacementTexture != old_displacementTexture)
        {
            old_displacementTexture = displacementTexture;
            displacementTextureColors = displacementTexture?.GetPixels32();
            retr = true;
        }
        */

        return retr;
    }
    public bool CheckClosedLoopToggled()
    {
        if (CheckFieldChanged(isClosedLoop, ref old_isClosedLoop))
        {
            positionCurve.Recalculate();
            UICurve.Initialize();
            return true;
        }
        return false;
    }

    public void ResetCurve()
    {
        positionCurve.Initialize();
        UICurve.Initialize();
    }
    [ContextMenu("Clear")]
    public void Clear()
    {
        positionCurve = new BezierCurve();
        positionCurve.owner = this;
        positionCurve.Initialize();
        positionCurve.isCurveOutOfDate = true;
        sizeSampler = new FloatDistanceSampler("Size", 1, EditMode.Size);
        rotationSampler = new FloatDistanceSampler("Rotation", 0, EditMode.Rotation);
        GUIStyle dropdownStyle = "ShurikenDropdown";
        doubleBezierSampler = new DoubleBezierSampler("Double Bezier", EditMode.DoubleBezier);
        UICurve = new UICurve(null, this);
        UICurve.Initialize();
        Debug.Log("cleared");
    }
    public void TryInitialize()
    {
        if (!isInitialized)
        {
            isInitialized = true;
            ResetCurve();
        }
    }
    public void Recalculate()
    {
        positionCurve.Recalculate();
        var secondaryCurves = doubleBezierSampler.points;
        if (secondaryCurves.Count > 0)
        {
            foreach (var curr in secondaryCurves)
                curr.value.owner = this;//gotta be careful that I'm not referencing stuff in owner that I shouldn't be
            var referenceHint = secondaryCurves[0].value.Recalculate();
            for (int i = 1; i < secondaryCurves.Count; i++)
                referenceHint = secondaryCurves[i].value.Recalculate(referenceHint);
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
            {EditMode.Arc, "Arc" },
            {EditMode.Thickness, "Thickness" },
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
public enum ValueType
{
    Constant=0,
    Keyframes=1
}
public enum InterpolationMode
{
    Linear = 0,
    Flat = 1,
}
public enum EditMode
{
    PositionCurve = 0,
    Rotation = 1,
    Size = 2,
    DoubleBezier = 3,
    Color = 4,
    Arc = 5,
    Thickness = 6,
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
