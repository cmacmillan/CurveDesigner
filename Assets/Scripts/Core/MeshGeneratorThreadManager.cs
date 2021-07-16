using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public static class MeshGeneratorThreadManager
    {
        private static Thread meshGenerationThread;
        private static object locker;
        private static ConcurrentQueue<MeshGeneratorData> generationQueue;
        private static ConcurrentDictionary<int, MeshGeneratorOutput> resultDict;
        public static void AddMeshGenerationRequest(Curve3D curve)
        {
            bool isFirstRun = false;
            if (meshGenerationThread == null)
            {
                isFirstRun = true;
                meshGenerationThread = new Thread(ThreadLoop);
                locker = new object();
                generationQueue = new ConcurrentQueue<MeshGeneratorData>();
            }

            curve.meshDispatchID++;
            MeshGeneratorData data = new MeshGeneratorData();
            data.curve = new BezierCurve(curve.positionCurve, false);
            data.currentlyGeneratingForCurveId = curve.GetInstanceID();
            data.RingPointCount = curve.ringPointCount;
            data.FlatPointCount = curve.flatPointCount;
            data.VertexSampleDistance = curve.GetVertexDensityDistance();
            data.tubeArcSampler = new FloatSampler(curve.arcOfTubeSampler, false, null);
            data.sizeSampler = new FloatSampler(curve.sizeSampler, false, null);
            data.rotationSampler = new FloatSampler(curve.rotationSampler, false, null);
            data.colorSampler = new ColorSampler(curve.colorSampler, false, null);
            data.extrudeSampler = new ExtrudeSampler(curve.extrudeSampler, false, null);
            data.thicknessSampler = new FloatSampler(curve.thicknessSampler, false, null);
            data.clampAndStretchMeshToCurve = curve.clampAndStretchMeshToCurve;
            data.mainTextureLayer = curve.mainTextureLayer.settings;
            data.backTextureLayer = curve.backTextureLayer.settings;
            data.edgeTextureLayer = curve.edgeTextureLayer.settings;
            data.endTextureLayer = curve.endTextureLayer.settings;
            data.edgeTextureLayer = curve.edgeTextureLayer.settings;
            data.IsClosedLoop = curve.isClosedLoop;
            data.CurveType = curve.type;
            data.meshToTile = curve.meshToTile == null ? null : new ThreadMesh(curve.meshToTile);
            data.closeTilableMeshGap = curve.closeTilableMeshGap;
            data.meshPrimaryAxis = curve.meshPrimaryAxis;
            data.meshDispatchID = curve.meshDispatchID;

            generationQueue.Enqueue(data);
            if (isFirstRun)
                meshGenerationThread.Start();
            else
                Monitor.PulseAll(locker);
        }
        public static void ThreadLoop()
        {
            while (true)
            {
                if (generationQueue.TryDequeue(out MeshGeneratorData nextData))
                {
                    try
                    {
                        var result = MeshGenerator.GenerateMesh(nextData);
                        resultDict.TryAdd(nextData.currentlyGeneratingForCurveId, result);
                    }
                    catch (Exception e)
                    {
                        resultDict.TryAdd(nextData.currentlyGeneratingForCurveId, null);//a null in the dictionary means something went wrong
                    }
                }
                else
                {
                    Monitor.Wait(locker);
                }
            }
        }
    }
    public class MeshGeneratorOutput
    {
        public List<Vector3> vertices = new List<Vector3>();
        public List<Vector2> uvs = new List<Vector2>();
        public List<Color32> colors = new List<Color32>();
        public List<List<int>> submeshes;
        public int submeshCount = 0;
        public void InitSubmeshes(int numSubmeshes)
        {
            submeshCount = numSubmeshes;
            if (submeshes == null)
                submeshes = new List<List<int>>();
            for (int i = submeshes.Count; i < numSubmeshes; i++)
                submeshes.Add(new List<int>());
            foreach (var i in submeshes)
                i.Clear();
        }
    }
    public class MeshGeneratorData
    {
        public ExtrudeSampler extrudeSampler;
        public FloatSampler sizeSampler;
        public FloatSampler rotationSampler;
        public ColorSampler colorSampler;
        public FloatSampler tubeArcSampler;
        public FloatSampler thicknessSampler;

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

        public int meshDispatchID;//essentially just to prevent re-copying over the data over and over again
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
            bounds = meshToCopy.bounds;
        }
        public void WriteToMesh(Mesh meshToWriteTo)
        {
            meshToWriteTo.vertices = verts;
            meshToWriteTo.triangles = tris;
            meshToWriteTo.normals = normals;
            meshToWriteTo.uv = uv;
        }
        public int[] tris;
        public Vector3[] verts;
        public Vector3[] normals;
        public Vector2[] uv;
        public Bounds bounds;
        //currently only supports uv0
    }

}
