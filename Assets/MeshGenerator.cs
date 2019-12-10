using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public static class MeshGenerator
{
    public static Vector3[] vertices;
    public static int[] triangles;

    public static bool IsBuzy = false;

    public static DateTime lastUpdateTime;

    public static BeizerCurve curve;

    private const int RingPointCount = 8;//temporary, replace with something customizable
    private const float radius=3.0f;//temporary
    private const float vertexDensity=.1f;

    public static void StartGenerating(BeizerCurve curve,DateTime now)
    {
        if (!IsBuzy)
        {
            IsBuzy = true;
            BeizerCurve clonedCurve = new BeizerCurve(curve);
            lastUpdateTime = now;
            MeshGenerator.curve = clonedCurve;
            Thread thread = new Thread(GenerateMesh);
            thread.Start();
        }
    }
    private static Vector3 NormalTangent(Vector3 forwardVector, Vector3 previous)
    {
        return Vector3.ProjectOnPlane(previous, forwardVector).normalized;
    }
    private static void GenerateMesh()
    {
        Debug.Log("started thread");
        curve.CacheSampleCurve(vertexDensity);//yuck, rewrite
        Vector3[] points = curve.GetCachedSampled(vertexDensity).Select(a => a.position).ToArray();//yuck, rewrite
        {//generate verts
            int numVerts = RingPointCount * points.Length;
            Vector3[] verts = new Vector3[numVerts];
            void GenerateRing(int i, Vector3 startPoint, Vector3 forwardVector, ref Vector3 previousTangent)
            {
                int ringIndex = i * RingPointCount;
                Vector3 tangentVect = NormalTangent(forwardVector, previousTangent);
                previousTangent = tangentVect;
                for (int j = 0; j < RingPointCount; j++)
                {
                    float theta = 360.0f * j / (float)RingPointCount;
                    Vector3 rotatedVect = Quaternion.AngleAxis(theta, forwardVector) * tangentVect;
                    verts[ringIndex + j] = startPoint + rotatedVect * radius;
                }
            }
            Vector3 lastTangent = Quaternion.FromToRotation(Vector3.forward, (points[1] - points[0]).normalized) * Vector3.right;
            for (int i = 0; i < points.Length - 1; i++)
            {
                GenerateRing(i, points[i], (points[i + 1] - points[i]).normalized, ref lastTangent);
            }
            int finalIndex = points.Length - 1;
            GenerateRing(finalIndex, points[finalIndex], (points[finalIndex] - points[finalIndex - 1]).normalized, ref lastTangent);
            vertices = verts;
        }
        {//generate tris
            int numRings = points.Length - 1;
            int numTris = RingPointCount * numRings * 6;//each ring point except for the last ring has a quad (6) associated with it
            int[] tris = new int[numTris];
            int triIndex = 0;
            void DrawQuad(int ring1Point1, int ring1Point2, int ring2Point1, int ring2Point2)
            {
                //Tri1
                tris[triIndex++] = ring1Point1;
                tris[triIndex++] = ring2Point2;
                tris[triIndex++] = ring2Point1;
                //Tri2
                tris[triIndex++] = ring1Point1;
                tris[triIndex++] = ring1Point2;
                tris[triIndex++] = ring2Point2;
            }
            for (int i = 0; i < points.Length - 1; i++)
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
            triangles = tris;
        }
        Debug.Log("finished thread");
        IsBuzy = false;
    }
}
