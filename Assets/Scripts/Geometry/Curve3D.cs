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
    private void OnDrawGizmos()
    {
        doubleBezierSampler.CacheOpenCurvePoints(positionCurve);
        void DoFor(float distance,float secondaryDistance)
        {
            var primaryCurvePoint = positionCurve.GetPointAtDistance(distance);
            var sample = doubleBezierSampler.SampleAt(distance, secondaryDistance, positionCurve, out Vector3 reference);
            var cross = Vector3.Cross(primaryCurvePoint.tangent, primaryCurvePoint.reference).normalized;
            Vector3 TransformVector3(Vector3 vect)
            {
                return (Quaternion.LookRotation(primaryCurvePoint.tangent,primaryCurvePoint.reference)*vect);
            }
            Gizmos.DrawRay(primaryCurvePoint.position+TransformVector3(sample),TransformVector3(reference)*300);
            //Gizmos.DrawRay(primaryCurvePoint.position+TransformVector3(sample),TransformVector3(Vector3.right)*300);  
        }
        DoFor(primaryDistance*positionCurve.GetLength(),secondaryDistance);
        //Gizmos.DrawRay(TransformVector3(sample) + primaryCurvePoint.position, TransformVector3(reference)*30);

       //Gizmos.DrawRay(primaryCurvePoint.position,TransformVector3(Vector3.right)*300);  
        foreach (var i in doubleBezierSampler.secondaryCurves)
        {
            DoFor(i.GetDistance(positionCurve),secondaryDistance);
        }
    }
    [Range(0,1)]
    public float primaryDistance;
    [Range(0,1)]
    public float secondaryDistance;

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

    public DimensionLockMode lockToPositionZero;
    [SerializeField]
    [HideInInspector]
    private DimensionLockMode oldLockToPositionZero;

    [Min(0)]
    public float vertexDensity = 1.0f;
    [SerializeField]
    [HideInInspector]
    private float oldVertexDensity = -1;

    [Min(0)]
    public int doubleBezierSampleCount = 1;
    [SerializeField]
    [HideInInspector]
    private int oldDoubleBezierVertexDensity = -1;

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

    public float closeTilableMeshGap;
    [HideInInspector]
    [SerializeField]
    private float oldCloseTilableMeshGap = -1;

    public Mesh meshToTile;
    [HideInInspector]
    [SerializeField]
    private Mesh oldMeshToTile=null;

    public bool isClosedLoop = false;
    [SerializeField]
    [HideInInspector]
    private bool oldIsClosedLoop;

    public MeshPrimaryAxis meshPrimaryAxis = MeshPrimaryAxis.auto;
    [SerializeField]
    [HideInInspector]
    private MeshPrimaryAxis oldMeshPrimaryAxis;

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

        retr|=CheckField(ringPointCount, ref oldRingPointCount);
        retr|=CheckField(vertexDensity, ref oldVertexDensity);
        retr|=CheckField(curveRadius, ref oldCurveRadius);
        retr|=CheckField(arcOfTube, ref oldArcOfTube);
        retr|=CheckField(curveRotation, ref oldCurveRotation);
        retr|=CheckField(thickness, ref oldTubeThickness);
        retr|=CheckField(type, ref oldType);
        retr|=CheckField(closeTilableMeshGap, ref oldCloseTilableMeshGap);
        retr|=CheckField(meshToTile, ref oldMeshToTile);
        retr|=CheckField(meshPrimaryAxis,ref oldMeshPrimaryAxis);
        retr|=CheckField(doubleBezierSampleCount, ref oldDoubleBezierVertexDensity);
        retr|=CheckField(lockToPositionZero, ref oldLockToPositionZero);

        if (CheckField(isClosedLoop, ref oldIsClosedLoop))
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
