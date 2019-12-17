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

    //public static List<IFieldKeyframe<float>> SizeKeyframes;
    public static AnimationCurve sizeCurve;

    public static int RingPointCount = 8;
    public static float Radius=3.0f;
    public static float VertexDensity=1.0f;
    public static float TubeAngle = 360.0f;
    public static float Rotation = 0.0f;

    private static void CopyOverKeyframeList<T,U>(List<U> sourceList, ref List<IFieldKeyframe<T>> destinationList) where U : IFieldKeyframe<T>
    {
        if (destinationList == null)
        {
            destinationList = new List<IFieldKeyframe<T>>();
        } else
        {
            destinationList.Clear();
        }
        if (destinationList.Capacity < sourceList.Count)
            destinationList.Capacity = sourceList.Count;
        foreach (var i in sourceList)
        {
            destinationList.Add(i.Clone());
        }
    }

    /*private static T GetKeyframeValueAtTime<T>(AnimationCurve keyframeCurve, ref int currentIndex,float distanceSinceIndex, out float currentKeyframeDistance)
    {
        float KeyframeLength(int index)
        {
            if (index == keyframes.Count - 1)
                return curve.GetLength() - keyframes[index].Distance;
            return keyframes[index + 1].Distance - keyframes[index].Distance;
        }
        while (currentIndex<keyframes.Count-1 && distanceSinceIndex > KeyframeLength(currentIndex))
        {
            distanceSinceIndex -= KeyframeLength(currentIndex);
            currentIndex++;
        }
        currentKeyframeDistance = keyframes[currentIndex].Distance;
        if (currentIndex == keyframes.Count - 1)
            return keyframes[currentIndex].Value;
        return keyframes[currentIndex].Lerp(keyframes[currentIndex+1],distanceSinceIndex/KeyframeLength(currentIndex));
    }*/

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
            //CopyOverKeyframeList(curve.curveSize,ref MeshGenerator.SizeKeyframes);
            MeshGenerator.sizeCurve = Curve3D.CopyAnimationCurve(curve.curveSizeAnimationCurve);

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
        var sampled = curve.GetCachedSampled();

        int numVerts = RingPointCount * sampled.Count;
        InitOrClear(ref vertices, numVerts);
        int numRings = sampled.Count - 1;
        int numTris = RingPointCount * numRings * 6;//each ring point except for the last ring has a quad (6) associated with it
        InitOrClear(ref triangles,numTris);

        {//generate verts
            float distanceFromFull = 360.0f - TubeAngle;
            int sizeIndexCache = 0;
            float previousSizeKeyframeDistance=0.0f;
            void GenerateRing(int i, SampleFragment startPoint, Vector3 forwardVector, ref Vector3 previousTangent)
            {
                int ringIndex = i * RingPointCount;
                //Old Method: Vector3 tangentVect = NormalTangent(forwardVector, previousTangent);
                Vector3 tangentVect = NormalTangent(forwardVector, Vector3.up);
                previousTangent = tangentVect;
                //var size = GetKeyframeValueAtTime(sizeCurve,ref sizeIndexCache,startPoint.distanceAlongCurve-previousSizeKeyframeDistance,out previousSizeKeyframeDistance);
                var size = sizeCurve.Evaluate(startPoint.distanceAlongCurve);
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
                GenerateRing(i, sampled[i], (sampled[i + 1].position - sampled[i].position).normalized, ref lastTangent);
            }
            int finalIndex = sampled.Count- 1;
            GenerateRing(finalIndex, sampled[finalIndex], (sampled[finalIndex].position - sampled[finalIndex - 1].position).normalized, ref lastTangent);
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
            for (int i = 0; i < sampled.Count- 1; i++)
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
