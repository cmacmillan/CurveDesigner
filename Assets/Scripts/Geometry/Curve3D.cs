using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace ChaseMacMillan.CurveDesigner
{
    [RequireComponent(typeof(MeshFilter))]
    public class Curve3D : MonoBehaviour, ISerializationCallbackReceiver
    {
        public IEnumerable<IDistanceSampler> DistanceSamplers
        {
            get
            {
                yield return sizeSampler;
                yield return rotationSampler;
                yield return extrudeSampler;
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
                    case EditMode.Extrude:
                        return extrudeSampler;
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
        [NonSerialized]
        public Matrix4x4 clipSpaceToWorldSpace;
        [NonSerialized]
        public Matrix4x4 worldSpaceToClipSpace;
        //sorted from most recent to oldest
        public List<SelectableGUID> selectedPoints = new List<SelectableGUID>();
        public ClickShiftControlState shiftControlState = ClickShiftControlState.none;

        private int meshGenerationId = -1;
        public int GetMeshGenerationID()
        {
            if (meshGenerationId == -1)
                meshGenerationId = MeshGenerator.GetCurve3DID();
            return meshGenerationId;
        }

        public enum ClickShiftControlState
        {
            none = 0,
            control = 1,
            shift = 2,
        }

        public void DeselectAllPoints()
        {
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
        //public CommandBuffer commandBuffer;

        #region guistyles
        private bool stylesInitialized = false;
        public void TryInitStyles()
        {
            if (stylesInitialized)
                return;
            buttonStyle = "Button";
            dropdownStyle = "ShurikenDropdown";
            particleLabelStyle = "ShurikenLabel";
            effectBgStyle = "ShurikenEffectBg";
            shurikenModuleBg = "ShurikenModuleBg";
            mixedToggleStyle = "ShurikenToggleMixed";
            initialHeaderStyle = "ShurikenEmitterTitle";
            nonInitialHeaderStyle = "ShurikenModuleTitle";

            if (controlRectStyle == null)
                controlRectStyle = new GUIStyle { margin = new RectOffset(0, 0, 2, 2) };

            centeredStyle = GUI.skin.GetStyle("Label");
            centeredStyle.alignment = TextAnchor.UpperCenter;
            stylesInitialized = true;
        }

        public GUIStyle centeredStyle;
        public GUIStyle buttonStyle;
        public GUIStyle particleLabelStyle;
        public GUIStyle controlRectStyle;
        public GUIStyle effectBgStyle;
        public GUIStyle shurikenModuleBg;
        public GUIStyle mixedToggleStyle;
        public GUIStyle initialHeaderStyle;
        public GUIStyle nonInitialHeaderStyle;
        public GUIStyle dropdownStyle;
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
            //We need to rebuild the guid map
            var dict = guidFactory.Objects;
            dict.Clear();
            foreach (var i in DistanceSamplers)
                foreach (var j in i.AllPoints())
                    dict.Add(j.GUID, j);
            foreach (var i in extrudeSampler.points)
                foreach (var j in i.value.PointGroups)
                    dict.Add(j.GUID, j);
            foreach (var i in positionCurve.PointGroups)
                dict.Add(i.GUID, i);
        }
        public IEnumerable<T> GetSelected<T>(List<SelectableGUID> selected) where T : class, ISelectable
        {
            return guidFactory.GetSelected<T>(selected);
        }
        public IEnumerable<ISelectable> GetSelected(List<SelectableGUID> selected)
        {
            return guidFactory.GetSelected(selected);
        }

        public FloatDistanceSampler sizeSampler;
        public FloatDistanceSampler arcOfTubeSampler;
        public FloatDistanceSampler thicknessSampler;
        public FloatDistanceSampler rotationSampler;
        public ColorDistanceSampler colorSampler;
        public ExtrudeSampler extrudeSampler;

        public void RequestMeshUpdate()
        {
            lastMeshUpdateStartTime = DateTime.Now;
        }

        [HideInInspector]
        public float averageSize;
        [HideInInspector]
        [NonSerialized]
        public DateTime lastMeshUpdateStartTime;
        [NonSerialized]
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
            positionCurve.normalGenerationMode = normalGenerationMode;
        }


        [NonSerialized]
        public ClickHitData elementClickedDown;
        [NonSerialized]
        public UICurve UICurve = null;

        public bool showPositionHandles = false;
        public bool showPointSelectionWindow = true;
        public bool showNormals = false;
        public bool showTangents = false;

        public EditMode editMode = EditMode.PositionCurve;

        [SerializeField]
        public Curve3dSettings _settings;
        public Curve3dSettings settings
        {
            get
            {
                if (_settings == null)
                {
                    _settings = Resources.Load<Curve3dSettings>("CurveDesignerSettings");
                }
                return _settings;
            }
        }

        public MeshCollider collider;
        public MeshFilter filter;
        [FormerlySerializedAs("mesh")]
        public Mesh displayMesh;
        [SerializeField]
        [HideInInspector]
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

        public CurveNormalGenerationMode normalGenerationMode;
        [SerializeField]
        [HideInInspector]
        private CurveNormalGenerationMode old_normalGenerationMode;

        [Min(0)]
        public float vertexDensity = 1.0f;
        [SerializeField]
        [HideInInspector]
        private float old_vertexDensity = -1;

        [Min(2)]
        [Tooltip("How accuratly should the curve perform length calculations? Increase to improve accuracy, decrease to improve speed")]
        public int samplesPerSegment = 50;
        [SerializeField]
        [HideInInspector]
        private float old_samplesPerSegment = -1;

        [Min(1)]
        public int samplesForCursorCollisionCheck = 6;
        [SerializeField]
        [HideInInspector]
        private int old_samplesForCursorCollisionCheck;

        [Min(3)]
        public int ringPointCount = 8;
        [SerializeField]
        [HideInInspector]
        private int old_ringPointCount = -1;

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
        private Mesh old_meshToTile = null;

        public bool isClosedLoop = false;
        [SerializeField]
        [HideInInspector]
        private bool old_isClosedLoop;

        public MeshPrimaryAxis meshPrimaryAxis = MeshPrimaryAxis.auto;
        [SerializeField]
        [HideInInspector]
        private MeshPrimaryAxis old_meshPrimaryAxis;

        //need to serialize this stuff
        public TextureLayer capTextureLayer;

        public TextureLayer flatTextureLayer;
        public TextureLayer alternateFlatTextureLayer;

        public TextureLayer tubeTextureLayer;
        public TextureLayer alternateTubeTextureLayer;
        

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

            void CheckSamplerChanged<T>(IValueSampler<T> sampler, ref T oldConst, ref ValueType oldInterpolation)
            {
                retr |= CheckFieldChanged(sampler.ConstValue, ref oldConst);
                retr |= CheckFieldChanged(sampler.ValueType, ref oldInterpolation);
            }
            retr |= CheckFieldChanged(ringPointCount, ref old_ringPointCount);
            retr |= CheckFieldChanged(vertexDensity, ref old_vertexDensity);
            retr |= CheckFieldChanged(type, ref old_type);
            retr |= CheckFieldChanged(closeTilableMeshGap, ref old_closeTilableMeshGap);
            if (meshToTile != null)
                retr |= CheckFieldChanged(meshToTile, ref old_meshToTile);
            retr |= CheckFieldChanged(meshPrimaryAxis, ref old_meshPrimaryAxis);
            retr |= CheckFieldChanged(seperateInnerOuterTextures, ref old_seperateInnerOuterTextures);
            retr |= CheckFieldChanged(clampAndStretchMeshToCurve, ref old_clampAndStretchMeshToCurve);
            retr |= CheckFieldChanged(normalGenerationMode, ref old_normalGenerationMode);
            retr |= CheckFieldChanged(samplesPerSegment, ref old_samplesPerSegment);
            retr |= CheckFieldChanged(samplesForCursorCollisionCheck, ref old_samplesForCursorCollisionCheck);
            bool didDimensionLockChange = CheckFieldChanged(lockToPositionZero, ref old_lockToPositionZero);
            retr |= didDimensionLockChange;
            if (didDimensionLockChange)
            {
                foreach (var i in positionCurve.PointGroups)
                {
                    i.SetWorldPositionByIndex(PGIndex.LeftTangent, i.GetWorldPositionByIndex(PGIndex.LeftTangent));
                    i.SetWorldPositionByIndex(PGIndex.Position, i.GetWorldPositionByIndex(PGIndex.Position));
                    i.SetWorldPositionByIndex(PGIndex.RightTangent, i.GetWorldPositionByIndex(PGIndex.RightTangent));
                }
            }

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

        [ContextMenu("Clear")]
        public void Clear()
        {
            filter = GetComponent<MeshFilter>();
            sizeSampler = new FloatDistanceSampler("Size", 1, EditMode.Size, 0);
            rotationSampler = new FloatDistanceSampler("Rotation", 0, EditMode.Rotation);
            arcOfTubeSampler = new FloatDistanceSampler("Arc", 180, EditMode.Arc, 0, 360);
            thicknessSampler = new FloatDistanceSampler("Thickness", .1f, EditMode.Thickness, 0);
            colorSampler = new ColorDistanceSampler("Color", EditMode.Color);
            extrudeSampler = new ExtrudeSampler("Extrude", EditMode.Extrude);
            positionCurve = new BezierCurve();
            positionCurve.owner = this;
            positionCurve.Initialize();
            positionCurve.isCurveOutOfDate = true;
            UICurve = new UICurve(null, this);
            UICurve.Initialize();
        }
        public void TryInitialize()
        {
            if (!isInitialized)
            {
                isInitialized = true;
                Clear();
            }
        }
        public void Recalculate()
        {
            positionCurve.Recalculate();
            var secondaryCurves = extrudeSampler.points;
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
            if (points.Count == 0 || sizeSampler.ValueType == ValueType.Constant)
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
        public float GetVertexDensityDistance() { return DensityToDistance(vertexDensity); }
        private const float normalValueLengthDivisor = 2.0f;
        private const float normalGapSizeMultiplier = 2.0f;
        public float VisualNormalsLength()
        {
            return positionCurve.GetLength() / 30;
        }
        public float GetNormalDensityDistance() { return VisualNormalsLength() * normalGapSizeMultiplier; }
    }
    public class EditModeCategories
    {
        public Dictionary<EditMode, string> editmodeNameMap = new Dictionary<EditMode, string>()
        {
            {EditMode.PositionCurve, "Position"},
            {EditMode.Size, "Size"},
            {EditMode.Rotation, "Rotation"},
            {EditMode.Extrude, "Extrude"},
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
    [System.Serializable]
    public class TextureLayer
    {
        public Material material;
        //Probably also need a texturing mode?
        //And maybe a secondary stretch mode or something
    }
    public enum ValueType
    {
        Constant = 0,
        Keyframes = 1
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
        Extrude = 3,
        Color = 4,
        Arc = 5,
        Thickness = 6,
    }
    public enum CurveType
    {
        HollowTube = 0,
        Flat = 1,
        Extrude = 2,
        NoMesh = 3,
        Cylinder = 4,
        Mesh = 5,
    }

}
