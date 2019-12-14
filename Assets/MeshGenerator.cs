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
    private static List<Vector3> points;

    public static bool IsBuzy = false;

    public static DateTime lastUpdateTime;

    public static BeizerCurve curve;

    public static int RingPointCount = 8;//temporary, replace with something customizable
    public static float Radius=3.0f;//temporary
    public static float VertexDensity=1.0f;
    public static float TubeAngle = 360.0f;
    public static float Rotation = 0.0f;

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
        Debug.Log("started thread");
        curve.CacheSampleCurve(VertexDensity);

        InitOrClear(ref points);
        int numVerts = RingPointCount * points.Count;
        InitOrClear(ref vertices, numVerts);
        int numRings = points.Count - 1;
        int numTris = RingPointCount * numRings * 6;//each ring point except for the last ring has a quad (6) associated with it
        InitOrClear(ref triangles,numTris);
        var sampled = curve.GetCachedSampled();
        foreach (var i in sampled)
        {
            points.Add(i.position);
        }
        {//generate verts
            float distanceFromFull = 360.0f - TubeAngle;
            void GenerateRing(int i, Vector3 startPoint, Vector3 forwardVector, ref Vector3 previousTangent)
            {
                int ringIndex = i * RingPointCount;
                //Old Method: Vector3 tangentVect = NormalTangent(forwardVector, previousTangent);
                Vector3 tangentVect = NormalTangent(forwardVector, Vector3.up);
                previousTangent = tangentVect;
                for (int j = 0; j < RingPointCount; j++)
                {
                    float theta = (TubeAngle * j / (float)RingPointCount) + distanceFromFull / 2 + Rotation;
                    Vector3 rotatedVect = Quaternion.AngleAxis(theta, forwardVector) * tangentVect;
                    vertices.Add(startPoint + rotatedVect * Radius);
                }
            }
            Vector3 lastTangent = Quaternion.FromToRotation(Vector3.forward, (points[1] - points[0]).normalized) * Vector3.right;
            for (int i = 0; i < points.Count - 1; i++)
            {
                GenerateRing(i, points[i], (points[i + 1] - points[i]).normalized, ref lastTangent);
            }
            int finalIndex = points.Count- 1;
            GenerateRing(finalIndex, points[finalIndex], (points[finalIndex] - points[finalIndex - 1]).normalized, ref lastTangent);
        }
        {//generate tris

            int triIndex = 0;
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
            for (int i = 0; i < points.Count- 1; i++)
            {
                int ringIndex = i * RingPointCount;
                int nextRingIndex = ringIndex + RingPointCount;
                for (int j = 0; j < RingPointCount; j++)
                {
                    DrawQuad(
                         ringIndex + j,
                         ringIndex + ((j + 1) % RingPointCount),
                         nextRingIndex + j,
                         nextRingIndex + ((j + 1) % RingPointCount));
                }
            }
        }
        Debug.Log("finished thread");
        IsBuzy = false;
    }
}
