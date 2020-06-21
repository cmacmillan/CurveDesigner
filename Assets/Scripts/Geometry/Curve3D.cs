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
    public class TextureLayer
    {
        public void UpdatePixels()
        {
            albedoTexturePixels = albedoTexture.GetPixels();
        }
        [System.NonSerialized]
        public TextureUVBounds bounds;
        [System.NonSerialized]
        public Color[] albedoTexturePixels;
        public Texture2D albedoTexture;
        public override bool Equals(object obj)
        {
            if (!(obj is TextureLayer))
                return base.Equals(obj);
            var otherLayerItem =(TextureLayer)obj;
            return otherLayerItem.albedoTexture == albedoTexture;
        }
        public TextureLayer(TextureLayer layerToClone)
        {
            this.albedoTexture = layerToClone.albedoTexture;
        }
    }

    public bool useSeperateInnerAndOuterFaceTextures;
    [SerializeField]
    [HideInInspector]
    private bool old_useSeperateInnerAndOuterFaceTextures;

    public TextureLayer outerFaceTexture;
    [SerializeField]
    [HideInInspector]
    private TextureLayer old_outerFaceTexture;

    public float outerFaceTextureScale;
    [SerializeField]
    [HideInInspector]
    private float old_outerFaceTextureScale;

    public TextureLayer innerFaceTexture;
    [SerializeField]
    [HideInInspector]
    private TextureLayer old_innerFaceTexture;

    public float innerFaceTextureScale;
    [SerializeField]
    [HideInInspector]
    private float old_innerFaceTextureScale;

    public TextureLayer edgeTexture;
    [SerializeField]
    [HideInInspector]
    private TextureLayer old_edgeTexture;

    public float edgeTextureScale;
    [SerializeField]
    [HideInInspector]
    public float old_edgeTextureScale;

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

        retr|=CheckFieldChanged(ringPointCount, ref old_ringPointCount);
        retr|=CheckFieldChanged(vertexDensity, ref old_vertexDensity);
        retr|=CheckFieldChanged(curveRadius, ref old_curveRadius);
        retr|=CheckFieldChanged(arcOfTube, ref old_arcOfTube);
        retr|=CheckFieldChanged(curveRotation, ref old_curveRotation);
        retr|=CheckFieldChanged(thickness, ref old_thickness);
        retr|=CheckFieldChanged(type, ref old_type);
        retr|=CheckFieldChanged(closeTilableMeshGap, ref old_closeTilableMeshGap);
        retr|=CheckFieldChanged(meshToTile, ref old_meshToTile);
        retr|=CheckFieldChanged(meshPrimaryAxis,ref old_meshPrimaryAxis);
        retr|=CheckFieldChanged(doubleBezierSampleCount, ref old_doubleBezierVertexDensity);
        retr|=CheckFieldChanged(lockToPositionZero, ref old_lockToPositionZero);
        retr|=CheckFieldChanged(outerFaceTextureScale, ref old_outerFaceTextureScale);
        retr|=CheckFieldChanged(innerFaceTextureScale, ref old_innerFaceTextureScale);
        retr|=CheckFieldChanged(edgeTextureScale, ref old_edgeTextureScale);

        if (CheckFieldChanged(isClosedLoop, ref old_isClosedLoop))
        {
            retr = true;
            positionCurve.Recalculate();
            UICurve.Initialize();
        }
        return retr;
    }

    //Only settings that actually force a texture rebuild should go in here
    //All others should go into HaveCurveSettingsChanged
    public bool HaveTextureSettingsChanged()
    {
        bool retr = false;
        bool DidTextureLayerChange(TextureLayer curr, ref TextureLayer old)
        {
            if (!curr.Equals(old))
            {
                old = new TextureLayer(curr);
                return true;
            }
            return false;
        }
        retr|=CheckFieldChanged(useSeperateInnerAndOuterFaceTextures, ref old_useSeperateInnerAndOuterFaceTextures);
        retr|=DidTextureLayerChange(edgeTexture, ref old_edgeTexture);
        retr|=DidTextureLayerChange(innerFaceTexture, ref old_innerFaceTexture);
        retr|=DidTextureLayerChange(outerFaceTexture, ref old_outerFaceTexture);
        retr|=curveAlbedo == null;
        return retr;
    }

    public bool forceRebuild = false;
    public Texture2D curveAlbedo = null;
    [ContextMenu("Clearalbedo")]
    public void ClearCurveAlbedo()
    {
        if (curveAlbedo == null)
            return;
        DestroyImmediate(curveAlbedo);
        curveAlbedo = null;
    }
    public void RebuildTextures()
    {
        Debug.Log("Rebuilding!");
        forceRebuild = false;
        if (innerFaceTexture.albedoTexture == null && useSeperateInnerAndOuterFaceTextures)
            return;
        if (outerFaceTexture.albedoTexture == null)
            return;
        if (edgeTexture.albedoTexture == null)
            return;
        int textureSize = settings.textureSize;
        if (curveAlbedo == null)
            curveAlbedo = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        TextureUVBounds.PopulateUVBounds(this, out TextureUVBounds interior, out TextureUVBounds exterior, out TextureUVBounds edge);
        List<TextureLayer> textures = new List<TextureLayer>();
        edgeTexture.bounds = edge;
        innerFaceTexture.bounds = interior;
        outerFaceTexture.bounds = exterior;
        textures.Add(edgeTexture);
        textures.Add(outerFaceTexture);
        float pixelSize = 1.0f / textureSize;
        if (useSeperateInnerAndOuterFaceTextures)
            textures.Add(innerFaceTexture);
        foreach (var i in textures)
            i.UpdatePixels();
        var z = textures[0];
        textures.Sort((a, b) => (int)Mathf.Sign(a.bounds.yMinMax.x - b.bounds.yMinMax.x));
        z = textures[0];
        void SetPixel(Color32[] arr, int x, int y, Color32 val)
        {
            arr[textureSize * y + x] = val;
        }
        Color GetPixel(Color[] arr, int x, int y,int width)
        {
            return arr[width * y + x];
        }
        Color32[] albedoColors = new Color32[textureSize * textureSize];
        TextureLayer GetLayerByY(float y)
        {
            foreach (var i in textures)
            {
                var bounds = i.bounds.yMinMax;
                if (y >= bounds.x && y <= bounds.y)
                    return i;
            }
            throw new KeyNotFoundException();
        }
        Color SampleTexture(TextureLayer texture, Vector2 uv){
            int width = texture.albedoTexture.width;
            int height = texture.albedoTexture.height;
            int xFloor = Mathf.FloorToInt(uv.x * width);
            xFloor = Mathf.Min(xFloor,width-2);
            int xCeil = xFloor + 1;
            int yFloor = Mathf.FloorToInt(uv.y * height);
            yFloor = Mathf.Min(yFloor,height-2);
            int yCeil = yFloor + 1;
            float xGap = 1.0f / width;
            float yGap = 1.0f / height;
            float xLerp = (uv.x - xFloor * xGap) / xGap;
            float yLerp = (uv.y - yFloor * yGap) / yGap;
            var tex = texture.albedoTexturePixels;
            Color topLeftColor = GetPixel(tex,xFloor,yFloor,width);
            Color topRightColor = GetPixel(tex,xCeil,yFloor,width);
            Color bottomLeftColor = GetPixel(tex,xFloor,yCeil,width);
            Color bottomRightColor = GetPixel(tex,xCeil,yCeil,width);
            Color topLerp = Color.Lerp(topLeftColor,topRightColor,xLerp);
            Color bottomLerp = Color.Lerp(bottomLeftColor,bottomRightColor,xLerp);
            Color finalColor = Color.Lerp(topLerp,bottomLerp,yLerp);
            return finalColor;
        }
        for (int y = 0; y < textureSize; y++)
        {
            var currLayer = GetLayerByY(y * pixelSize);
            for (int x = 0; x < textureSize; x++)
            {
                var minMax = currLayer.bounds.yMinMax;
                float heightFactor = minMax.y - minMax.x;
                float yProgress = y * pixelSize;
                yProgress = (yProgress - minMax.x) / (minMax.y - minMax.x);
                Color32 colorToSet = SampleTexture(currLayer, new Vector2(x*pixelSize,yProgress));
                SetPixel(albedoColors, x, y, colorToSet);
            }
        }
        curveAlbedo.SetPixels32(albedoColors);
        curveAlbedo.Apply();
        this.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_MainTex",curveAlbedo);
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
