using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace ChaseMacMillan.CurveDesigner
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class Curve3D : MonoBehaviour, ISerializationCallbackReceiver
    {
        public IEnumerable<ISampler> DistanceSamplers
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
                    case Curve3DEditMode.PositionCurve:
                        return positionCurve;
                    case Curve3DEditMode.Rotation:
                        return rotationSampler;
                    case Curve3DEditMode.Size:
                        return sizeSampler;
                    case Curve3DEditMode.Extrude:
                        return extrudeSampler;
                    case Curve3DEditMode.Color:
                        return colorSampler;
                    case Curve3DEditMode.Thickness:
                        return thicknessSampler;
                    case Curve3DEditMode.Arc:
                        return arcOfTubeSampler;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public CollapsableCategory[] collapsableCategories =
        {
        new MainCollapsableCategory(),
        new MeshGenerationCollapsableCategory(),
        new TexturesCollapsableCategory(),
        new PreferencesCollapsableCategory(),
    };

        [NonSerialized]
        public Matrix4x4 clipSpaceToWorldSpace;
        [NonSerialized]
        public Matrix4x4 worldSpaceToClipSpace;
        //sorted from most recent to oldest
        public List<SelectableGUID> selectedPoints = new List<SelectableGUID>();
        public ClickShiftControlState shiftControlState = ClickShiftControlState.none;

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

            shurikenCustomDataWindow = new GUIStyle(GUI.skin.window);
            shurikenCustomDataWindow.font = UnityEditor.EditorStyles.miniFont;

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
        public GUIStyle shurikenCustomDataWindow;
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

        public FloatSampler sizeSampler;
        public FloatSampler arcOfTubeSampler;
        public FloatSampler thicknessSampler;
        public FloatSampler rotationSampler;
        public ColorSampler colorSampler;
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

        public Curve3DEditMode editMode = Curve3DEditMode.PositionCurve;

        [SerializeField]
        public GUITexturesAndStyles _settings;
        public GUITexturesAndStyles settings
        {
            get
            {
                if (_settings == null)
                {
                    _settings = Resources.Load<GUITexturesAndStyles>("CurveDesignerSettings");
                }
                return _settings;
            }
        }

        public MeshCollider collider;
        public MeshFilter Filter { 
            get
            {
                if (_filter == null)
                    _filter = GetComponent<MeshFilter>();
                return _filter;
            } 
        }
        public MeshRenderer Renderer
        {
            get
            {
                if (_renderer == null)
                    _renderer = GetComponent<MeshRenderer>();
                return _renderer;
            }
        }

        [SerializeField]
        private MeshFilter _filter;
        [SerializeField]
        private MeshRenderer _renderer;

        public Mesh displayMesh;

        public int meshCreatorId;

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
        private bool old_sizeUseKeyframes;

        [SerializeField]
        [HideInInspector]
        private bool old_rotationUseKeyframes;

        [SerializeField]
        [HideInInspector]
        private bool old_colorUseKeyframes;

        [SerializeField]
        [HideInInspector]
        private bool old_arcOfTubeUseKeyframes;

        [SerializeField]
        [HideInInspector]
        private bool old_thicknessUseKeyframes;

        public bool clampAndStretchMeshToCurve = true;
        [SerializeField]
        [HideInInspector]
        private bool old_clampAndStretchMeshToCurve;

        public DimensionLockMode lockToPositionZero;
        [SerializeField]
        [HideInInspector]
        private DimensionLockMode old_lockToPositionZero;

        public CurveNormalGenerationMode normalGenerationMode;
        [SerializeField]
        [HideInInspector]
        private CurveNormalGenerationMode old_normalGenerationMode;

        [Min(0)]
        public float vertexDensity = 40.0f;
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
        public int flatPointCount = 3;
        [SerializeField]
        [HideInInspector]
        private int old_flatPointCount = -1;

        [Min(3)]
        public int ringPointCount = 8;
        [SerializeField]
        [HideInInspector]
        private int old_ringPointCount = -1;

        public MeshGenerationMode type = MeshGenerationMode.HollowTube;
        [HideInInspector]
        [SerializeField]
        private MeshGenerationMode old_type;

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

        public bool ShouldUseMainTextureLayer() { return type != MeshGenerationMode.NoMesh && type != MeshGenerationMode.Mesh; }

        public TextureLayer mainTextureLayer = new TextureLayer(null);
        [SerializeField]
        [HideInInspector]
        private TextureLayer old_mainTextureLayer = new TextureLayer(null);
        public bool ShouldUseBackTextureLayer() { return type != MeshGenerationMode.NoMesh && type != MeshGenerationMode.Mesh; }

        public TextureLayer backTextureLayer = new TextureLayer(null);
        [SerializeField]
        [HideInInspector]
        private TextureLayer old_backTextureLayer = new TextureLayer(null);
        public bool ShouldUseEndTextureLayer() { return !isClosedLoop; }

        public TextureLayer endTextureLayer = new TextureLayer(null);
        [SerializeField]
        [HideInInspector]
        private TextureLayer old_endTextureLayer = new TextureLayer(null);

        public bool ShouldUseEdgeTextureLayer() { return type == MeshGenerationMode.HollowTube || type == MeshGenerationMode.Extrude || type == MeshGenerationMode.Flat; }

        public TextureLayer edgeTextureLayer = new TextureLayer(null);
        [SerializeField]
        [HideInInspector]
        private TextureLayer old_edgeTextureLayer = new TextureLayer(null);


        private bool CheckFieldChanged<T>(T field, ref T oldField)
        {
            if (!field.Equals(oldField))
            {
                oldField = field;
                return true;
            }
            return false;
        }
        public bool CheckTextureLayerChanged(TextureLayer curr, ref TextureLayer old)
        {
            bool changed = curr.material != old.material || 
                           curr.settings.textureGenMode != old.settings.textureGenMode ||
                           curr.settings.textureDirection != old.settings.textureDirection ||
                           curr.settings.scale != old.settings.scale;
            if (changed)
                old = curr;
            return changed;
        }

        public bool HaveCurveSettingsChanged()
        {
            bool retr = false;

            void CheckSamplerChanged<T>(IValueSampler<T> sampler, ref T oldConst, ref bool oldInterpolation)
            {
                retr |= CheckFieldChanged(sampler.ConstValue, ref oldConst);
                retr |= CheckFieldChanged(sampler.UseKeyframes, ref oldInterpolation);
            }
            retr |= CheckFieldChanged(ringPointCount, ref old_ringPointCount);
            retr |= CheckFieldChanged(flatPointCount, ref old_flatPointCount);
            retr |= CheckFieldChanged(vertexDensity, ref old_vertexDensity);
            retr |= CheckFieldChanged(type, ref old_type);
            retr |= CheckFieldChanged(closeTilableMeshGap, ref old_closeTilableMeshGap);
            if (meshToTile != null)
                retr |= CheckFieldChanged(meshToTile, ref old_meshToTile);
            retr |= CheckFieldChanged(meshPrimaryAxis, ref old_meshPrimaryAxis);
            retr |= CheckFieldChanged(clampAndStretchMeshToCurve, ref old_clampAndStretchMeshToCurve);
            retr |= CheckFieldChanged(normalGenerationMode, ref old_normalGenerationMode);
            retr |= CheckFieldChanged(samplesPerSegment, ref old_samplesPerSegment);
            retr |= CheckFieldChanged(samplesForCursorCollisionCheck, ref old_samplesForCursorCollisionCheck);

            retr |= CheckTextureLayerChanged(mainTextureLayer, ref old_mainTextureLayer);
            retr |= CheckTextureLayerChanged(backTextureLayer, ref old_backTextureLayer);
            retr |= CheckTextureLayerChanged(endTextureLayer, ref old_endTextureLayer);
            retr |= CheckTextureLayerChanged(edgeTextureLayer, ref old_edgeTextureLayer);

            bool didDimensionLockChange = CheckFieldChanged(lockToPositionZero, ref old_lockToPositionZero);
            retr |= didDimensionLockChange;
            if (didDimensionLockChange)
            {
                foreach (var i in positionCurve.PointGroups)
                {
                    i.SetWorldPositionByIndex(PointGroupIndex.LeftTangent, i.GetWorldPositionByIndex(PointGroupIndex.LeftTangent));
                    i.SetWorldPositionByIndex(PointGroupIndex.Position, i.GetWorldPositionByIndex(PointGroupIndex.Position));
                    i.SetWorldPositionByIndex(PointGroupIndex.RightTangent, i.GetWorldPositionByIndex(PointGroupIndex.RightTangent));
                }
            }

            CheckSamplerChanged(colorSampler, ref old_constColor, ref old_colorUseKeyframes);
            CheckSamplerChanged(sizeSampler, ref old_constSize, ref old_sizeUseKeyframes);
            CheckSamplerChanged(rotationSampler, ref old_constRotation, ref old_rotationUseKeyframes);
            CheckSamplerChanged(arcOfTubeSampler, ref old_constArcOfTube, ref old_arcOfTubeUseKeyframes);
            CheckSamplerChanged(thicknessSampler, ref old_constThickness, ref old_thicknessUseKeyframes);

            retr |= CheckClosedLoopToggled();

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
            sizeSampler = new FloatSampler("Size", .2f, Curve3DEditMode.Size, 0);
            rotationSampler = new FloatSampler("Rotation", 0, Curve3DEditMode.Rotation);
            arcOfTubeSampler = new FloatSampler("Arc", 180, Curve3DEditMode.Arc, 0, 360);
            thicknessSampler = new FloatSampler("Thickness", .05f, Curve3DEditMode.Thickness, 0);
            colorSampler = new ColorSampler("Color", Curve3DEditMode.Color);
            extrudeSampler = new ExtrudeSampler("Extrude", Curve3DEditMode.Extrude);
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
                var mat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");
                mainTextureLayer.material = mat;
                backTextureLayer.material = mat;
                edgeTextureLayer.material = mat;
                endTextureLayer.material = mat;
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

        public void UpdateMaterials()
        {
            List<Material> mats = new List<Material>();
            if (ShouldUseMainTextureLayer())
                mats.Add(mainTextureLayer.material);
            if (ShouldUseBackTextureLayer())
                mats.Add(backTextureLayer.material);
            if (ShouldUseEdgeTextureLayer())
                mats.Add(edgeTextureLayer.material);
            if (ShouldUseEndTextureLayer())
                mats.Add(endTextureLayer.material);
            Renderer.materials = mats.ToArray();
        }
        public void CacheAverageSize()
        {
            float avg = 0;
            var points = sizeSampler.GetPoints(this.positionCurve);
            if (points.Count == 0 || !sizeSampler.UseKeyframes)
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
        public Dictionary<Curve3DEditMode, string> editmodeNameMap = new Dictionary<Curve3DEditMode, string>()
        {
            {Curve3DEditMode.PositionCurve, "Position"},
            {Curve3DEditMode.Size, "Size"},
            {Curve3DEditMode.Rotation, "Rotation"},
            {Curve3DEditMode.Extrude, "Extrude"},
            {Curve3DEditMode.Color, "Color" },
            {Curve3DEditMode.Arc, "Arc" },
            {Curve3DEditMode.Thickness, "Thickness" },
        };
        public Curve3DEditMode[] editModes;
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
            var baseEditModes = System.Enum.GetValues(typeof(Curve3DEditMode));
            var baseEditModeNames = System.Enum.GetNames(typeof(Curve3DEditMode));
            editModes = new Curve3DEditMode[baseEditModes.Length];
            for (int i = 0; i < editModes.Length; i++)
                editModes[i] = (Curve3DEditMode)baseEditModes.GetValue(i);
        }
    }
    [System.Serializable]
    public struct TextureLayer
    {
        public TextureLayer(Material m)
        {
            material = m;
            settings = new TextureLayerSettings() { scale = 1, textureDirection = TextureDirection.x, textureGenMode = TextureGenerationMode.Tile };
        }
        public Material material;
        public TextureLayerSettings settings;
    }
    [System.Serializable]
    public struct TextureLayerSettings
    {
        public TextureGenerationMode textureGenMode;
        public TextureDirection textureDirection;
        public float scale;
    }
}
