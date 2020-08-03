using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public struct MeshGenerationJob : IJob
{
    private float[] fuckle;
    public bool didMeshGenerationSucceed;
    public NativeArray<Vector3> vertices;
    public NativeArray<int> triangles;
    public NativeArray<Vector2> uvs;
    public NativeArray<Color32> colors;

    public bool IsBuzy; //= false;

    //public DateTime lastUpdateTime;

    public BezierCurve curve;

    /*
    public DoubleBezierSampler doubleBezierSampler;
    public FloatDistanceSampler sizeSampler;
    public FloatDistanceSampler rotationSampler;
    public ColorDistanceSampler colorSampler;
    */

    public int RingPointCount;// = 8;
    public float Radius;//=3.0f;
    public float VertexSampleDistance;// = 1.0f;
    public float TubeArc;// = 360.0f;
    public float Rotation;// = 0.0f;

    public float Thickness;// = 0.0f;
    public bool IsClosedLoop;// = false;
    public CurveType CurveType;

    public float closeTilableMeshGap;
    public MeshPrimaryAxis meshPrimaryAxis;

    //should really use a custom native container to group all this crap, but I don't wanna deal with figuring that out now
    //https://docs.unity3d.com/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeContainerAttribute.html
    //public ThreadMesh meshToTile;
    public NativeArray<int> meshToTileTris;
    public NativeArray<Vector3> meshToTileVerts;
    public NativeArray<Vector3> meshToTileNormals;
    public NativeArray<Vector2> meshToTileUVS;
    public Bounds meshToTileBounds;

    public void CopyFromMesh(Mesh meshToCopy,
                             out NativeArray<int> tris, 
                             out NativeArray<Vector3> verts, 
                             out NativeArray<Vector2> uv, 

                             out NativeArray<Vector3> normals,
                             out Bounds bounds
                             )
    {
        void Init<T>(out NativeArray<T> native, T[] arr) where T : struct
        {
            native = new NativeArray<T>();
            native.CopyFrom(arr);
        }
        Init(out tris, meshToCopy.triangles);
        Init(out verts, meshToCopy.vertices);
        Init(out uv, meshToCopy.uv);
        Init(out normals, meshToCopy.normals);
        bounds = meshToCopy.bounds;
    }

    public void CopyToMesh(Mesh meshToWriteTo,
                           NativeArray<int> tris, 
                           NativeArray<Vector3> verts, 
                           NativeArray<Vector2> uv, 
                           NativeArray<Vector3> normals,
                           Bounds bounds
                             )
    {
        meshToWriteTo.vertices = verts.ToArray();
        meshToWriteTo.triangles = tris.ToArray();
        meshToWriteTo.normals = normals.ToArray();
        meshToWriteTo.uv = uv.ToArray();
    }

    public void Execute()
    {
        //Unity.Collections.LowLevel.Unsafe.UnsafeUtility.IsBlittable<>
        throw new System.NotImplementedException();
    }
}
