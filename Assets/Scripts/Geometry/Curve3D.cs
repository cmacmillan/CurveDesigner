using Assets.NewUI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using static BezierCurve;

public class Curve3D : MonoBehaviour
{
    public IEnumerable<FloatLinearDistanceSampler> DistanceSamplers
    {
        get
        {
            yield return sizeDistanceSampler;
            yield return rotationDistanceSampler;
        }
    }


    public Mesh testMesh;
    public Material testMat;
    public CommandBuffer commandBuffer;

    public bool placeLockedPoints = true;
    public SplitInsertionNeighborModification splitInsertionBehaviour = SplitInsertionNeighborModification.DoNotModifyNeighbors;

    [ContextMenu("ExportToObj")]
    public void ExportToObj()
    {
        ObjMeshExporter.DoExport(gameObject, false);
    }

    [HideInInspector]
    public FloatLinearDistanceSampler sizeDistanceSampler = new FloatLinearDistanceSampler();
    [HideInInspector]
    public FloatLinearDistanceSampler rotationDistanceSampler = new FloatLinearDistanceSampler();
    [HideInInspector]
    public DoubleBezierSampler doubleBezierSampler = new DoubleBezierSampler();

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

    public bool drawNormals = true;

    public EditMode editMode=EditMode.PositionCurve;

    public Curve3dSettings settings;

    public MeshFilter filter;
    [FormerlySerializedAs("mesh")]
    public Mesh displayMesh;
    private bool isInitialized = false;

    /// Start of properties that redraw the curve

    [System.Serializable]
    public struct TextureLayerItem
    {
        public Texture albedoTexture;
        public float scale;
        public override bool Equals(object obj)
        {
            if (!(obj is TextureLayerItem))
                return base.Equals(obj);
            var otherLayerItem =(TextureLayerItem)obj;
            return otherLayerItem.albedoTexture == albedoTexture &&
                   otherLayerItem.scale == scale;
        }
    }

    public bool useSeperateInnerAndOuterFaceTextures;
    [SerializeField]
    [HideInInspector]
    private bool old_useSeperateInnerAndOuterFaceTextures;

    public TextureLayerItem outerFaceTexture;
    [SerializeField]
    [HideInInspector]
    private TextureLayerItem old_outerFaceTexture;

    public TextureLayerItem innerFaceTexture;
    [SerializeField]
    [HideInInspector]
    private TextureLayerItem old_innerFaceTexture;

    public TextureLayerItem edgeTexture;
    [SerializeField]
    [HideInInspector]
    private TextureLayerItem old_edgeTexture;

    public DimensionLockMode lockToPositionZero;
    [SerializeField]
    [HideInInspector]
    private DimensionLockMode old_lockToPositionZero;

    [Min(0)]
    public float vertexDensity = 1.0f;
    [SerializeField]
    [HideInInspector]
    private float old_vertexDensity = -1;

    [Min(0)]
    public int doubleBezierSampleCount = 1;
    [SerializeField]
    [HideInInspector]
    private int old_doubleBezierVertexDensity = -1;

    [Min(0)]
    public float curveRadius =3.0f;
    [SerializeField]
    [HideInInspector]
    private float old_curveRadius=-1;

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

    [Range(0, 360)]
    public float curveRotation = 360;
    [SerializeField]
    [HideInInspector]
    private float old_curveRotation= -1;

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

        bool retr = false;

        retr|=CheckField(ringPointCount, ref old_ringPointCount);
        retr|=CheckField(vertexDensity, ref old_vertexDensity);
        retr|=CheckField(curveRadius, ref old_curveRadius);
        retr|=CheckField(arcOfTube, ref old_arcOfTube);
        retr|=CheckField(curveRotation, ref old_curveRotation);
        retr|=CheckField(thickness, ref old_thickness);
        retr|=CheckField(type, ref old_type);
        retr|=CheckField(closeTilableMeshGap, ref old_closeTilableMeshGap);
        retr|=CheckField(meshToTile, ref old_meshToTile);
        retr|=CheckField(meshPrimaryAxis,ref old_meshPrimaryAxis);
        retr|=CheckField(doubleBezierSampleCount, ref old_doubleBezierVertexDensity);
        retr|=CheckField(lockToPositionZero, ref old_lockToPositionZero);
        retr|=CheckField(useSeperateInnerAndOuterFaceTextures, ref old_useSeperateInnerAndOuterFaceTextures);
        retr|=CheckField(edgeTexture, ref old_edgeTexture);
        retr|=CheckField(innerFaceTexture, ref old_innerFaceTexture);
        retr|=CheckField(outerFaceTexture, ref old_outerFaceTexture);

        if (CheckField(isClosedLoop, ref old_isClosedLoop))
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
        foreach (var i in rotationDistanceSampler.GetPoints(this))
            previousRotations.Add(i.value);
    }
    public void CacheAverageSize()
    {
        float avg = 0;
        var points = sizeDistanceSampler.GetPoints(this);
        averageSize = 0;
        if (points.Count > 0)
        {
            foreach (var i in points)
                avg += i.value;
            averageSize = avg / points.Count;
        }
        averageSize += curveRadius;
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
public enum EditMode
{
    PositionCurve = 0,
    Rotation = 1,
    Size = 2,
    DoubleBezier = 3,
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
