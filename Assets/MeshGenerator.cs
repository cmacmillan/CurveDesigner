using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public static class MeshGenerator
{
    public static List<Vector3> vertices;
    public static List<int> triangles;

    public static bool IsBuzy = false;

    public static DateTime lastUpdateTime;

    public static BeizerCurve curve;

    public static TubeType TubeType;

    public static AnimationCurve sizeCurve;

    public static int RingPointCount = 8;
    public static float Radius=3.0f;
    public static float VertexDensity=1.0f;
    public static float TubeAngle = 360.0f;
    public static float Rotation = 0.0f;
    public static float TubeThickness = 0.0f;

    public static void StartGenerating(Curve3D curve)
    {
        if (!IsBuzy)
        {
            IsBuzy = true;
            BeizerCurve clonedCurve = new BeizerCurve(curve.positionCurve);
            lastUpdateTime = curve.lastMeshUpdateStartTime;

            MeshGenerator.curve = clonedCurve;
            MeshGenerator.RingPointCount = curve.ringPointCount;
            MeshGenerator.Radius = curve.curveRadius;
            MeshGenerator.VertexDensity = curve.curveVertexDensity;
            MeshGenerator.TubeAngle = curve.angleOfTube;
            MeshGenerator.Rotation = curve.curveRotation;
            MeshGenerator.TubeType = curve.tubeType;
            MeshGenerator.sizeCurve = Curve3D.CopyAnimationCurve(curve.curveSizeAnimationCurve);
            MeshGenerator.TubeThickness = curve.tubeThickness;

            Thread thread = new Thread(GenerateMesh);
            thread.Start();
        }
    }
    private static Vector3 NormalTangent(Vector3 forwardVector, Vector3 previous)
    {
        return Vector3.ProjectOnPlane(previous, forwardVector).normalized;
    }
    private static void InitOrClear<T>(ref List<T> list,int capacity=-1)
    {
        if (list == null)
        {
            if (capacity<=0)
                list = new List<T>();
            else 
                list = new List<T>(capacity);
        }
        else
        {
            list.Clear();
            if (capacity > list.Capacity)
                list.Capacity = capacity;
        }
    }
    private static void GenerateMesh()
    {
        //Debug.Log("started thread");
        curve.CacheSampleCurve(VertexDensity);
        var sampled = curve.GetCachedSampled();
        int numVerts;
        int numTris;
        int numRings = sampled.Count - 1;

        void GenerateVertexLayer(bool isExterior){//generate verts
            float distanceFromFull = 360.0f - TubeAngle;
            void GenerateRing(SampleFragment startPoint, Vector3 forwardVector, ref Vector3 previousTangent)
            {
                //Old Method: 
                //Vector3 tangentVect = NormalTangent(forwardVector, previousTangent);
                Vector3 tangentVect = NormalTangent(forwardVector, Vector3.up);
                previousTangent = tangentVect;
                float offset = (isExterior ? .5f :-.5f)*(TubeThickness);
                var size = sizeCurve.Evaluate(startPoint.distanceAlongCurve)+offset;
                for (int j = 0; j < RingPointCount; j++)
                {
                    float theta = (TubeAngle * j / (float)RingPointCount) + distanceFromFull / 2 + Rotation;
                    Vector3 rotatedVect = Quaternion.AngleAxis(theta, forwardVector) * tangentVect;
                    vertices.Add(startPoint.position + rotatedVect * size);
                }
            }
            Vector3 lastTangent = Quaternion.FromToRotation(Vector3.forward, (sampled[1].position - sampled[0].position).normalized) * Vector3.right;
            for (int i = 0; i < sampled.Count - 1; i++)
            {
                GenerateRing(sampled[i], (sampled[i + 1].position - sampled[i].position).normalized, ref lastTangent);
            }
            int finalIndex = sampled.Count- 1;
            GenerateRing(sampled[finalIndex], (sampled[finalIndex].position - sampled[finalIndex - 1].position).normalized, ref lastTangent);
        }
        void TrianglifyLayer(bool isExterior){//generate tris
            void DrawQuad(int ring1Point1, int ring1Point2, int ring2Point1, int ring2Point2)
            {
                //Tri1
                triangles.Add(ring1Point1);
                triangles.Add(ring2Point2);
                triangles.Add(ring2Point1);
                //Tri2
                triangles.Add(ring1Point1);
                triangles.Add(ring1Point2);
                triangles.Add(ring2Point2);
            }
            int basePoint = isExterior ? 0 :numVerts/2;
            for (int i = basePoint; i < sampled.Count- + basePoint; i++)
            {
                int ringIndex = i * RingPointCount;
                int nextRingIndex = ringIndex + RingPointCount;
                for (int j = 0; j < RingPointCount; j++)
                {
                    if (isExterior)
                        DrawQuad(
                            ringIndex + j,
                            ringIndex + ((j + 1) % RingPointCount),
                            nextRingIndex + j,
                            nextRingIndex + ((j + 1) % RingPointCount));
                    else //flipped
                        DrawQuad(
                            ringIndex + ((j + 1) % RingPointCount),
                            ringIndex + j,
                            nextRingIndex + ((j + 1) % RingPointCount),
                            nextRingIndex + j);
                }
            }
        }
        void InitLists()
        {
            InitOrClear(ref vertices, numVerts);
            InitOrClear(ref triangles,numTris);
        }
        switch (TubeType)
        {
            case TubeType.Solid:
                numVerts= RingPointCount * sampled.Count;
                numTris=RingPointCount * numRings * 6;//each ring point except for the last ring has a quad (6) associated with it
                InitLists();
                GenerateVertexLayer(true);
                TrianglifyLayer(true);
                break;
            case TubeType.Hollow:
                numVerts = RingPointCount * sampled.Count * 2;
                numTris = RingPointCount * numRings * 6 * 2;
                InitLists();
                GenerateVertexLayer(true);
                GenerateVertexLayer(false);
                TrianglifyLayer(true);
                TrianglifyLayer(false);
                break;
        }
        IsBuzy = false;
    }
}
