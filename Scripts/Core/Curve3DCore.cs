using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace ChaseMacMillan.CurveDesigner
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public partial class Curve3D : MonoBehaviour, ISerializationCallbackReceiver
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
                yield return normalSampler;
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
                    case Curve3DEditMode.Normal:
                        return normalSampler;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

#if UNITY_EDITOR
        public CollapsableCategory[] collapsableCategories =
        {
        new MainCollapsableCategory(),
        new QualityCollapsableCategory(),
        new TexturesCollapsableCategory(),
        new PreferencesCollapsableCategory(),
        new HelpCollapsableCategory(),
        new WarningCollapsableCategory()
    };
#endif

        [NonSerialized]
        public Matrix4x4 clipSpaceToWorldSpace;
        [NonSerialized]
        public Matrix4x4 worldSpaceToClipSpace;
        //sorted from most recent to oldest
        public List<SelectableGUID> selectedPoints = new List<SelectableGUID>();
        public bool isMultiselecting = false;

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
#if UNITY_EDITOR
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
#endif
        #endregion

        public bool placeLockedPoints = true;
        public SplitInsertionNeighborModification splitInsertionBehaviour = SplitInsertionNeighborModification.DoNotModifyNeighbors;

        public SelectableGUIDFactory guidFactory = new SelectableGUIDFactory();

        public void OnBeforeSerialize() { /* Do Nothing */ }

        public void OnAfterDeserialize()
        {
            try
            {
                Recalculate();
                foreach (var i in DistanceSamplers)
                    i.RecalculateOpenCurveOnlyPoints(positionCurve);
                //We need to rebuild the guid map
                var dict = guidFactory.Objects;
                dict.Clear();
                foreach (var i in DistanceSamplers)
                    foreach (var j in i.AllPoints())
                    {
                        dict.Add(j.GUID, j);
                    }
                foreach (var i in extrudeSampler.points)
                    foreach (var j in i.value.PointGroups)
                    {
                        dict.Add(j.GUID, j);
                    }
                foreach (var i in positionCurve.PointGroups)
                    dict.Add(i.GUID, i);
            } 
            catch (ArgumentException e)
            {
                //This catches a weird issue
                //Where if you undo, OnAfterDeserialization gets called multiple times, and the first few times the points haven't been full deserialized
                //Which leads to an ArgumentException for trying to insert a guid multiple times
            }
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
        public NormalSampler normalSampler;

        [NonSerialized]
        private bool isWaitingForMeshResults = false;

        [HideInInspector]
        public float averageSize;
        [HideInInspector]
        public List<float> previousRotations = new List<float>();
        [HideInInspector]
        public BezierCurve positionCurve;
        public void BindDataToPositionCurve()
        {
            positionCurve.owner = this;
            positionCurve.dimensionLockMode = lockToPositionZero;
        }


        [NonSerialized]
        public ClickHitData elementClickedDown;
        [NonSerialized]
        public UICurve UICurve = null;

        public bool showPositionHandles = true;
        public bool showPointSelectionWindow = true;
        public bool showNormals = false;
        public bool showTangents = false;

        public bool mainCategoryExpanded = true;
        public bool meshGenerationCategoryExpanded = false;
        public bool textureCategoryExpanded = false;
        public bool preferencesCategoryExpanded = false;
        public bool advancedCategoryExpanded = false;
        public bool helpCategoryExpanded = false;
        public bool warningCategoryExpanded = false;

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

        [HideInInspector]
        public List<ObjectOnCurve> objectsOnThisCurve = new List<ObjectOnCurve>();

        [SerializeField]
        private MeshFilter _filter;
        [SerializeField]
        private MeshRenderer _renderer;

        public Mesh displayMesh;
        public int displayMeshCreatorId=-1;

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

        [SerializeField]
        [HideInInspector]
        private bool old_normalUseKeyframes;

        [SerializeField]
        [HideInInspector]
        private Vector3 old_constNormal;

        public bool clampAndStretchMeshToCurve = true;
        [SerializeField]
        [HideInInspector]
        private bool old_clampAndStretchMeshToCurve;

        public DimensionLockMode lockToPositionZero;
        [SerializeField]
        [HideInInspector]
        private DimensionLockMode old_lockToPositionZero;

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

        [SerializeField]
        [HideInInspector]
        private bool old_isClosedLoop=false;

        [SerializeField]
        [HideInInspector]
        private bool old_automaticTangents;

        [SerializeField]
        [HideInInspector]
        private float old_automaticTangentSmoothing;

        public MeshPrimaryAxis meshPrimaryAxis = MeshPrimaryAxis.auto;
        [SerializeField]
        [HideInInspector]
        private MeshPrimaryAxis old_meshPrimaryAxis;

        public bool ShouldUseMainTextureLayer() { return type != MeshGenerationMode.NoMesh; }

        public TextureLayer mainTextureLayer = new TextureLayer(null);
        [SerializeField]
        [HideInInspector]
        private TextureLayer old_mainTextureLayer = new TextureLayer(null);
        public bool ShouldUseBackTextureLayer() { return type != MeshGenerationMode.NoMesh && type != MeshGenerationMode.Mesh; }

        public TextureLayer backTextureLayer = new TextureLayer(null);
        [SerializeField]
        [HideInInspector]
        private TextureLayer old_backTextureLayer = new TextureLayer(null);
        public bool ShouldUseEndTextureLayer() { return !positionCurve.isClosedLoop; }

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
        public void OnCurveTypeChanged()
        {
            WriteMaterialsToRenderer();
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
            retr |= CheckFieldChanged(samplesPerSegment, ref old_samplesPerSegment);

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
                    i.SetPositionLocal(PointGroupIndex.LeftTangent, i.GetPositionLocal(PointGroupIndex.LeftTangent));
                    i.SetPositionLocal(PointGroupIndex.Position, i.GetPositionLocal(PointGroupIndex.Position));
                    i.SetPositionLocal(PointGroupIndex.RightTangent, i.GetPositionLocal(PointGroupIndex.RightTangent));
                }
                positionCurve.Recalculate();
                UICurve.Initialize();
            }

            CheckSamplerChanged(colorSampler, ref old_constColor, ref old_colorUseKeyframes);
            CheckSamplerChanged(sizeSampler, ref old_constSize, ref old_sizeUseKeyframes);
            CheckSamplerChanged(rotationSampler, ref old_constRotation, ref old_rotationUseKeyframes);
            CheckSamplerChanged(arcOfTubeSampler, ref old_constArcOfTube, ref old_arcOfTubeUseKeyframes);
            CheckSamplerChanged(thicknessSampler, ref old_constThickness, ref old_thicknessUseKeyframes);

            if (old_normalUseKeyframes != normalSampler.UseKeyframes)
                positionCurve.Recalculate();

            CheckSamplerChanged(normalSampler, ref old_constNormal, ref old_normalUseKeyframes);

            if (CheckFieldChanged(positionCurve.automaticTangentSmoothing, ref old_automaticTangentSmoothing))
            {
                positionCurve.Recalculate();
                UICurve.Initialize();
                retr = true;
            }

            if (CheckFieldChanged(positionCurve.automaticTangents, ref old_automaticTangents))
            {
                positionCurve.Recalculate();
                UICurve.Initialize();
                retr = true;
            }

            if (CheckFieldChanged(positionCurve.isClosedLoop, ref old_isClosedLoop))
            {
                positionCurve.Recalculate();
                UICurve.Initialize();
                retr = true;
            }

            return retr;
        }

        [ContextMenu("UpgradeCurve")]
        private void UpgradeCurve()//delete me
        {
            //if (normalSampler==null)
            normalSampler = new NormalSampler("Normal Generation", Curve3DEditMode.Normal);
            CacheSamplerDistances();
            UICurve = new UICurve(null, this);
            UICurve.Initialize();
        }

        [ContextMenu("Clear")]
        public void Clear(bool updateMesh=true)
        {
            sizeSampler = new FloatSampler("Size", .2f, Curve3DEditMode.Size, 0);
            rotationSampler = new FloatSampler("Rotation", 0, Curve3DEditMode.Rotation);
            arcOfTubeSampler = new FloatSampler("Arc", 180, Curve3DEditMode.Arc, 0, 360);
            thicknessSampler = new FloatSampler("Thickness", .05f, Curve3DEditMode.Thickness, 0);
            colorSampler = new ColorSampler("Color", Curve3DEditMode.Color);
            normalSampler = new NormalSampler("Normal Generation", Curve3DEditMode.Normal);
            positionCurve = new BezierCurve();
            positionCurve.owner = this;
            positionCurve.Initialize();
            positionCurve.Recalculate();
            extrudeSampler = new ExtrudeSampler("Extrude", Curve3DEditMode.Extrude,positionCurve);
            UICurve = new UICurve(null, this);
            UICurve.Initialize();
        }
        public void Initialize(bool updateMesh = true)
        {
            if (!isInitialized)
            {
                Clear();
                var mat = settings.defaultMaterial;
                mainTextureLayer.material = mat;
                backTextureLayer.material = mat;
                edgeTextureLayer.material = mat;
                endTextureLayer.material = mat;
                WriteMaterialsToRenderer();
                isInitialized = true;
                if (updateMesh)
                    RequestMeshUpdate();
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

        public void CacheSamplerDistances()
        {
            foreach (var i in DistanceSamplers)
            {
                i.CacheDistances(positionCurve);
            }
        }

        public PointOnCurve GetClosestPointOnCurve(Vector3 worldPosition)
        { 
            Vector3 localPos = transform.InverseTransformPoint(worldPosition);
            ClosestPointOnCurve.GetClosestPoint(positionCurve, localPos, out int segmentIndex, out float time);
            float dist = positionCurve.GetDistanceAtSegmentIndexAndTime(segmentIndex, time);
            return GetPointAtDistanceAlongCurve(dist);
        }

        public void CopyRotations()
        {
            previousRotations.Clear();
            foreach (var i in rotationSampler.GetPoints(this.positionCurve))
                previousRotations.Add(i.value);
        }

        public void AppendCurve_Internal(Curve3D otherCurve)
        {
            if (otherCurve.type != type)
            {
                Debug.LogError("Curve types must match in order to append!");
                return;
            }
            if (IsClosedLoop || otherCurve.IsClosedLoop)
            {
                Debug.LogError("You cannot append closed loop curves!");
                return;
            }
            float thisCurveLength = positionCurve.GetLength();
            var thisEnd = GetPointAtDistanceAlongCurve(thisCurveLength,false);
            var otherStart = otherCurve.GetPointAtDistanceAlongCurve(0,false);
            var rotateTangent = Quaternion.FromToRotation(otherStart.tangent, thisEnd.tangent);
            var rotateReference = Quaternion.FromToRotation(rotateTangent*otherStart.reference, thisEnd.reference);
            var rotation = rotateReference * rotateTangent;
            var otherPointGroups = otherCurve.positionCurve.PointGroups;
            for (int i = 1; i < otherPointGroups.Count; i++)
            {
                var curr = otherPointGroups[i];
                Vector3 originalPosition = curr.GetPositionLocal(PointGroupIndex.Position)-otherStart.position;
                Vector3 position = rotation * originalPosition;
                position += thisEnd.position;
                positionCurve.AppendPoint(false, curr.GetIsPointLocked(), position);
                var point = positionCurve.PointGroups[positionCurve.PointGroups.Count-1];
                Vector3 tangent = curr.GetPositionLocal(PointGroupIndex.LeftTangent)-otherStart.position;
                point.SetPositionLocal(PointGroupIndex.LeftTangent,thisEnd.position+(rotation*tangent));
                if (!curr.GetIsPointLocked()) {//test me
                    Vector3 rightTangent = curr.GetPositionLocal(PointGroupIndex.RightTangent)-otherStart.position;
                    point.SetPositionLocal(PointGroupIndex.RightTangent,thisEnd.position+(rotation*rightTangent));
                }
            }
            Recalculate();//we have to recalculate so we can insert by distance

            sizeSampler.CopyFrom(otherCurve.sizeSampler,thisCurveLength,this,otherCurve);
            arcOfTubeSampler.CopyFrom(otherCurve.arcOfTubeSampler,thisCurveLength,this,otherCurve);
            thicknessSampler.CopyFrom(otherCurve.thicknessSampler,thisCurveLength,this,otherCurve);
            rotationSampler.CopyFrom(otherCurve.rotationSampler,thisCurveLength,this,otherCurve);
            colorSampler.CopyFrom(otherCurve.colorSampler,thisCurveLength,this,otherCurve);
            if (extrudeSampler!=null && otherCurve.extrudeSampler!=null)
                extrudeSampler.CopyFrom(otherCurve.extrudeSampler,thisCurveLength,this,otherCurve);

            //normalSampler.CopyFrom(otherCurve.normalSampler, thisCurveLength, this, otherCurve);
            foreach (var i in otherCurve.normalSampler.points)
            {
                NormalSamplerPoint newPoint = new NormalSamplerPoint();
                newPoint.Construct(i, normalSampler, true, this);
                newPoint.value = rotation*newPoint.value;
                normalSampler.points.Add(newPoint);
                newPoint.SetDistance(thisCurveLength+i.GetDistance(otherCurve.positionCurve), positionCurve,false);
            }
            normalSampler.Sort(positionCurve);
            Recalculate();//we have to recalculate again because our normal sampler changed

            foreach (var i in otherCurve.objectsOnThisCurve)
            {
                var objectOnCurve = Instantiate(i.gameObject, transform).GetComponent<ObjectOnCurve>();
                objectOnCurve.curve = this;
                objectOnCurve.lengthwisePosition += thisCurveLength;
            }

            if (UICurve!=null)
                UICurve.Initialize();
            RequestMeshUpdate();
            //and we must consider whether or not to use the sampler points, or just insert a new sampler point representing the const value of that sampler at the beginning with no interpolation after
        }

        public void ReadMaterialsFromRenderer()
        {
            int index = 0;
            var mats = Renderer.sharedMaterials;
            if (ShouldUseMainTextureLayer() && index < mats.Length)
                mainTextureLayer.material = mats[index++];
            if (ShouldUseBackTextureLayer() && index<mats.Length)
                backTextureLayer.material = mats[index++];
            if (ShouldUseEdgeTextureLayer() && index<mats.Length)
                edgeTextureLayer.material = mats[index++];
            if (ShouldUseEndTextureLayer() && index<mats.Length)
                endTextureLayer.material = mats[index++];
            if (mats.Length < NumSubmeshesByCurveType(type))
            {
                WriteMaterialsToRenderer();
            }
        }
        private bool wantsUpdate = false;
        public void RequestMeshUpdate()
        {
            if (!isWaitingForMeshResults)
            {
                if (type == MeshGenerationMode.Mesh)
                {
                    if (meshToTile == null)
                        return;
                    if (!meshToTile.isReadable)
                    {
                        Debug.LogError($"MeshToTile '{meshToTile.name}' must be marked readable! Enable read/write in the import settings!");
                        return;
                    }
                }
                wantsUpdate = false;
                MeshGeneratorThreadManager.AddMeshGenerationRequest(this);
                isWaitingForMeshResults = true;
            }
            else
            {
                wantsUpdate = true;
            }
        }
        public void UpdateMesh(bool checkIfSettingsChanged=true)
        {
            if ((checkIfSettingsChanged && HaveCurveSettingsChanged()) || wantsUpdate)
            {
                RequestMeshUpdate();
            }
            //we only update the mesh if the thread manager has results for us
            if (MeshGeneratorThreadManager.GetMeshResults(this,out MeshGeneratorOutput output))
            {
                isWaitingForMeshResults = false;
                if (output == null)//an error happened, but we are still done
                    return;
                if (type == MeshGenerationMode.NoMesh)
                {
                    displayMesh.Clear();
                }
                else
                {
                    if (displayMesh == null || displayMeshCreatorId!=GetInstanceID())
                    {
                        displayMesh = new Mesh();
                        displayMesh.indexFormat = IndexFormat.UInt32;
                        Filter.mesh = displayMesh;
                        displayMeshCreatorId = GetInstanceID();
                        displayMesh.MarkDynamic();
                    }
                    else
                    {
                        displayMesh.Clear();
                    }
#if UNITY_2019_3_OR_NEWER
                    //use fast api if possible
                    displayMesh.SetVertexBufferParams(output.data.Count,MeshGeneratorThreadManager.GetVertexBufferParams());
                    displayMesh.SetVertexBufferData(output.data, 0, 0, output.data.Count);
                    displayMesh.SetIndexBufferParams(output.triangles.Count, IndexFormat.UInt32);
                    displayMesh.SetIndexBufferData(output.triangles, 0, 0, output.triangles.Count, MeshUpdateFlags.DontValidateIndices);
                    displayMesh.subMeshCount = output.submeshInfo.Count;
                    for (int i = 0; i < output.submeshInfo.Count; i++)
                    {
                        var sub = output.submeshInfo[i];
                        var descriptor = new SubMeshDescriptor(sub.indexStart, sub.indexCount);
                        descriptor.firstVertex = sub.firstVertex;
                        descriptor.vertexCount = sub.vertexCount;
                        descriptor.bounds = output.bounds;
                        descriptor.baseVertex = 0;
                        displayMesh.SetSubMesh(i, descriptor, MeshUpdateFlags.DontRecalculateBounds);
                    }
                    displayMesh.bounds = output.bounds;
#else
                    //fallback to old api
                    var positions = MeshGeneratorThreadManager.positionCopyList;
                    var normals = MeshGeneratorThreadManager.normalCopyList;
                    var tangents = MeshGeneratorThreadManager.tangentCopyList;
                    var uv = MeshGeneratorThreadManager.uvCopyList;
                    var colors = MeshGeneratorThreadManager.colorCopyList;
                    var triangles = MeshGeneratorThreadManager.triangleCopyList;
                    positions.Clear();
                    normals.Clear();
                    tangents.Clear();
                    uv.Clear();
                    colors.Clear();
                    foreach (var i in output.data)
                    {
                        positions.Add(i.position);
                        normals.Add(i.normal);
                        tangents.Add(i.tangent);
                        uv.Add(i.uv);
                        colors.Add(i.color);
                    }
                    displayMesh.SetVertices(positions);
                    displayMesh.SetNormals(normals);
                    displayMesh.SetTangents(tangents);
                    displayMesh.SetColors(colors);
                    displayMesh.SetUVs(0, uv);
                    int subMeshCount = output.submeshInfo.Count;
                    displayMesh.subMeshCount = subMeshCount;
                    int submeshIndex = 0;
                    foreach (var i in output.submeshInfo)
                    {
                        int end = i.indexStart + i.indexCount;
                        triangles.Clear();
                        for (int c=i.indexStart;c<end;c++)
                        {
                            triangles.Add(output.triangles[c]);
                        }
                        displayMesh.SetTriangles(triangles,submeshIndex);
                        submeshIndex++;
                    }
#endif
                    if (collider == null)
                        collider = GetComponent<MeshCollider>();
                    if (collider != null)
                        collider.sharedMesh = displayMesh;
                }
                MeshGeneratorThreadManager.ReturnToOutputPool(output);
            }
        }
        private int NumSubmeshesByCurveType(MeshGenerationMode type)
        {
            switch (type)
            {
                case MeshGenerationMode.Cylinder:
                    return 3;
                default:
                    return 4;
            }
        }
        public void WriteMaterialsToRenderer()
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
            Renderer.sharedMaterials = mats.ToArray();
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
            {Curve3DEditMode.Normal, "Normal" }
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
