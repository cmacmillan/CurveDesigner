using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

namespace ChaseMacMillan.CurveDesigner
{
    public static class MeshGeneratorThreadManager
    {
        private static Thread meshGenerationThread;
        private static readonly object locker = new object();
        private static readonly List<MeshGeneratorOutput> outputPool = new List<MeshGeneratorOutput>();//only access on main thread
        private static readonly ConcurrentQueue<MeshGeneratorData> generationQueue = new ConcurrentQueue<MeshGeneratorData>();
        private static readonly ConcurrentDictionary<int, MeshGeneratorOutput> resultDict = new ConcurrentDictionary<int, MeshGeneratorOutput>();
        public static void ReturnToOutputPool(MeshGeneratorOutput output)
        {
            output.data.Clear();
            output.distances.Clear();
            output.triangles.Clear();
            output.submeshInfo.Clear();
            outputPool.Add(output);
        }

        public static bool GetMeshResults(Curve3D curve,out MeshGeneratorOutput output)
        {
            if (resultDict.TryRemove(curve.GetInstanceID(),out output))
            {
                return true;
            } 
            else
            {
                return false;
            }
        }
        public static void AddMeshGenerationRequest(Curve3D curve)
        {
            bool isFirstRun = false;
            if (meshGenerationThread == null)
            {
                isFirstRun = true;
                meshGenerationThread = new Thread(ThreadLoop);
            }

            MeshGeneratorData data = new MeshGeneratorData();
            if (outputPool.Count == 0)
                data.output = new MeshGeneratorOutput();
            else
            {
                data.output = outputPool[outputPool.Count - 1];
                outputPool.RemoveAt(outputPool.Count - 1);
            }
            data.curve = new BezierCurve(curve.positionCurve, false);
            data.currentlyGeneratingForCurveId = curve.GetInstanceID();
            data.RingPointCount = curve.ringPointCount;
            data.FlatPointCount = curve.flatPointCount;
            data.VertexSampleDistance = curve.GetVertexDensityDistance();
            data.tubeArcSampler = new FloatSampler(curve.arcOfTubeSampler, false, null);
            data.sizeSampler = new FloatSampler(curve.sizeSampler, false, null);
            data.rotationSampler = new FloatSampler(curve.rotationSampler, false, null);
            data.colorSampler = new ColorSampler(curve.colorSampler, false, null);
            if (curve.type == MeshGenerationMode.Extrude)
                data.extrudeSampler = new ExtrudeSampler(curve.extrudeSampler, false, null);
            else
                data.extrudeSampler = null;
            data.thicknessSampler = new FloatSampler(curve.thicknessSampler, false, null);
            data.clampAndStretchMeshToCurve = curve.clampAndStretchMeshToCurve;
            data.mainTextureLayer = curve.mainTextureLayer.settings;
            data.backTextureLayer = curve.backTextureLayer.settings;
            data.edgeTextureLayer = curve.edgeTextureLayer.settings;
            data.endTextureLayer = curve.endTextureLayer.settings;
            data.edgeTextureLayer = curve.edgeTextureLayer.settings;
            data.IsClosedLoop = curve.IsClosedLoop;
            data.CurveType = curve.type;
            data.meshToTile = curve.meshToTile == null ? null : new ThreadMesh(curve.meshToTile);
            data.closeTilableMeshGap = curve.closeTilableMeshGap;
            data.meshPrimaryAxis = curve.meshPrimaryAxis;

            generationQueue.Enqueue(data);
            if (isFirstRun)
                meshGenerationThread.Start();
            else
            {
                lock (locker)
                {
                    Monitor.PulseAll(locker);
                }
            }
        }
        public static void ThreadLoop()
        {
            while (true)
            {
                MeshGeneratorData nextData;
                bool didDequeue;
                lock (locker)
                {
                    while (generationQueue.Count == 0) Monitor.Wait(locker);
                    didDequeue = generationQueue.TryDequeue(out nextData);
                }
                if (didDequeue)
                {
                    try
                    {
                        var result = MeshGenerator.GenerateMesh(nextData);
                        resultDict.TryAdd(nextData.currentlyGeneratingForCurveId, result);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogException(e);
                        resultDict.TryAdd(nextData.currentlyGeneratingForCurveId, null);//a null in the dictionary means something went wrong
                    }
                }
            }
        }

#if UNITY_2019_3_OR_NEWER
        private static VertexAttributeDescriptor[] vertexBufferParams;
        public static VertexAttributeDescriptor[] GetVertexBufferParams()
        {
            if (vertexBufferParams == null)
            {
                //must match data layout of MeshGeneratorVertexItem
                vertexBufferParams = new VertexAttributeDescriptor[] 
                { 
                    new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32,3),
                    new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
                    new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 4),
                    new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8,4),
                    new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
                };
            }
            return vertexBufferParams;
        }
#else
        public static List<Vector3> positionCopyList = new List<Vector3>();
        public static List<Vector3> normalCopyList = new List<Vector3>();
        public static List<Vector2> uvCopyList = new List<Vector2>();
        public static List<Vector4> tangentCopyList = new List<Vector4>();
        public static List<Color32> colorCopyList = new List<Color32>();
        public static List<int> triangleCopyList = new List<int>();
#endif
    }
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct MeshGeneratorVertexItem
    {
        public Vector3 position;//12 bytes
        public Vector3 normal;//12 bytes
        public Vector4 tangent;//16 bytes
        public Color32 color;//4 bytes 
        public Vector2 uv;//8 bytes
    }
    public class MeshGeneratorOutput
    {
        public MeshGeneratorOutput()
        {
        }
        public List<MeshGeneratorVertexItem> data = new List<MeshGeneratorVertexItem>();
        public List<SurfaceInfo> distances = new List<SurfaceInfo>();//stores the distance from the start of the curve for each vertex
        public List<int> triangles = new List<int>();
        public List<MySubMeshDescriptor> submeshInfo = new List<MySubMeshDescriptor>();
    }
    public struct MySubMeshDescriptor
    {
        public int indexStart;
        public int indexCount;
        public MySubMeshDescriptor(int indexStart, int indexCount)
        {
            this.indexStart = indexStart;
            this.indexCount = indexCount;
        }
#if UNITY_2019_3_OR_NEWER
     public SubMeshDescriptor GetDescriptor()
     {
        return new SubMeshDescriptor(indexStart,indexCount);
     }
#endif
    }
    public struct SurfaceInfo
    {
        public SurfaceInfo(float lengthwise, float crosswise)
        {
            this.lengthwise = lengthwise;
            this.crosswise = crosswise;
        }
        public float lengthwise;
        public float crosswise;
    }
    public class MeshGeneratorData
    {
        public ExtrudeSampler extrudeSampler;
        public FloatSampler sizeSampler;
        public FloatSampler rotationSampler;
        public ColorSampler colorSampler;
        public FloatSampler tubeArcSampler;
        public FloatSampler thicknessSampler;

        public MeshGeneratorOutput output;

        public BezierCurve curve;

        public TextureLayerSettings mainTextureLayer;
        public TextureLayerSettings backTextureLayer;
        public TextureLayerSettings edgeTextureLayer;
        public TextureLayerSettings endTextureLayer;

        public int RingPointCount = 2;
        public int FlatPointCount = 2;
        public int EdgePointCount = 20;
        public float VertexSampleDistance = 1.0f;
        public bool clampAndStretchMeshToCurve = true;
        public bool IsClosedLoop = false;
        public MeshGenerationMode CurveType;
        public ThreadMesh meshToTile;
        public float closeTilableMeshGap;
        public MeshPrimaryAxis meshPrimaryAxis;

        public int currentlyGeneratingForCurveId;
    }
    public class ThreadMesh
    {
        public ThreadMesh(Mesh meshToCopy)
        {
            tris = meshToCopy.triangles;
            verts = meshToCopy.vertices;
            uv = meshToCopy.uv;
            normals = meshToCopy.normals;
            tangents = meshToCopy.tangents;
            bounds = meshToCopy.bounds;
        }
        public void WriteToMesh(Mesh meshToWriteTo)
        {
            meshToWriteTo.vertices = verts;
            meshToWriteTo.triangles = tris;
            meshToWriteTo.normals = normals;
            meshToWriteTo.tangents = tangents;
            meshToWriteTo.uv = uv;
        }
        public int[] tris;
        public Vector3[] verts;
        public Vector3[] normals;
        public Vector4[] tangents;
        public Vector2[] uv;
        public Bounds bounds;
        //currently only supports uv0
    }

}
