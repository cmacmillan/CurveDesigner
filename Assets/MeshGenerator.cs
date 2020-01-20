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
    public static float TubeArc = 360.0f;
    public static bool IsTubeArcConstant { get { return true; } }
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
            MeshGenerator.TubeArc = curve.arcOfTube;
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
        int numVerts;
        int numTris;
        bool shouldDrawConnectingFace;
        var sampled = curve.GetPoints();
        int numRings = sampled.Count - 1;
        void GenerateVertexLayer(bool isExterior){//generate verts
            float distanceFromFull = 360.0f - TubeArc;
            void GenerateRing(PointOnCurve startPoint, Vector3 forwardVector, ref Vector3 previousTangent)
            {
                //Old Method: 
                //Vector3 tangentVect = NormalTangent(forwardVector, previousTangent);
                Vector3 tangentVect = NormalTangent(forwardVector, Vector3.up);
                previousTangent = tangentVect;
                float offset = (isExterior ? .5f :-.5f)*(TubeThickness);
                var size = Mathf.Max(0, sizeCurve.Evaluate(startPoint.distanceFromStartOfCurve) + offset);
                for (int j = 0; j < RingPointCount; j++)
                {
                    float theta = (TubeArc * j / (RingPointCount-(TubeArc==360.0?0:1))) + distanceFromFull / 2 + Rotation;
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
        void DrawQuad(int side1Point1, int side1Point2, int side2Point1, int side2Point2)
        {
            //Tri1
            triangles.Add(side1Point1);
            triangles.Add(side2Point2);
            triangles.Add(side2Point1);
            //Tri2
            triangles.Add(side1Point1);
            triangles.Add(side1Point2);
            triangles.Add(side2Point2);
        }
        void DrawTri(int point1, int point2, int point3)
        {
            triangles.Add(point1);
            triangles.Add(point2);
            triangles.Add(point3);
        }
        void TrianglifyLayer(bool isExterior)
        {//generate tris
            int basePoint = isExterior ? 0 :numVerts/2;
            for (int i = 0; i < numRings; i++)
            {
                int ringIndex = i * RingPointCount;
                int nextRingIndex = ringIndex + RingPointCount;
                for (int j = 0; j < RingPointCount; j++)
                {
                    if (!shouldDrawConnectingFace && (j + 1) >= RingPointCount)//will introduce a bug where curve never closes, even when angle is 360 TODO: revist
                        continue;
                    if (isExterior)
                        DrawQuad(
                            ringIndex + j+basePoint,
                            ringIndex + ((j + 1) % RingPointCount)+basePoint,
                            nextRingIndex + j+basePoint,
                            nextRingIndex + ((j + 1) % RingPointCount)+basePoint);
                    else //flipped
                        DrawQuad(
                            ringIndex + ((j + 1) % RingPointCount)+basePoint,
                            ringIndex + j+basePoint,
                            nextRingIndex + ((j + 1) % RingPointCount)+basePoint,
                            nextRingIndex + j+basePoint);
                }
            }
        }
        void InitLists()
        {
            InitOrClear(ref vertices, numVerts);
            InitOrClear(ref triangles,numTris);
        }
        void ConnectTubeInteriorAndExterior()
        {
            int interiorBase = numVerts / 2;
            for (int i = 0; i < numRings; i++)
            {
                int ringIndex = i * RingPointCount;
                int nextRingIndex = ringIndex + RingPointCount;
                DrawQuad(
                        ringIndex,
                        nextRingIndex,
                        interiorBase+ringIndex,
                        interiorBase+nextRingIndex
                    );
                DrawQuad(
                        nextRingIndex+RingPointCount-1,
                        ringIndex+RingPointCount-1,
                        interiorBase+nextRingIndex+RingPointCount-1,
                        interiorBase+ringIndex+RingPointCount-1
                    );
            }
        }
        void ConnectTubeInteriorExteriorEnds()
        {
            int interiorBase = numVerts / 2;
            //Then we gotta connect the ends as well
            int lastRingIndex = numRings * RingPointCount;
            int firstRingIndex = 0;
            for (int j = 0; j < RingPointCount; j++)
            {
                if (!shouldDrawConnectingFace && (j + 1) >= RingPointCount)//will introduce a bug where curve never closes, even when angle is 360 TODO: revist
                    continue;
                DrawQuad(
                        firstRingIndex + ((j + 1) % RingPointCount),
                        firstRingIndex+ j,
                        firstRingIndex + ((j + 1) % RingPointCount) + interiorBase,
                        firstRingIndex + j + interiorBase
                    );
                DrawQuad(
                        lastRingIndex+ j,
                        lastRingIndex + ((j + 1) % RingPointCount),
                        lastRingIndex + j + interiorBase,
                        lastRingIndex + ((j + 1) % RingPointCount) + interiorBase
                    );
            }
        }
        void CreateTubeEndPlates()
        {
            //center point is average of ring
            int AddRingCenterVertexFromAverage(int baseIndex)
            {
                Vector3 average= Vector3.zero;
                for (int i = 0; i < RingPointCount; i++)
                    average += vertices[i+baseIndex];
                average = average/ RingPointCount;
                vertices.Add(average);
                return vertices.Count - 1;
            }
            int startRingBaseIndex = 0;
            int endRingBaseIndex = numRings* RingPointCount;
            void TrianglifyRingToCenter(int baseIndex,int centerIndex,bool invert)
            {
                for (int i = 0; i < RingPointCount; i++)
                {
                    if (invert)
                        DrawTri(
                            baseIndex + i,
                            centerIndex,
                            baseIndex + ((i + 1) % RingPointCount)
                            );
                    else
                        DrawTri(
                            baseIndex + i,
                            baseIndex + ((i + 1) % RingPointCount),
                            centerIndex
                            );
                }
            }
            TrianglifyRingToCenter(startRingBaseIndex, AddRingCenterVertexFromAverage(startRingBaseIndex),true);
            TrianglifyRingToCenter(endRingBaseIndex, AddRingCenterVertexFromAverage(endRingBaseIndex),false);
        }
        switch (TubeType)
        {
            case TubeType.Solid:
                numVerts = RingPointCount * sampled.Count;
                numTris = RingPointCount * numRings * 6;//each ring point except for the last ring has a quad (6) associated with it
                shouldDrawConnectingFace = true;
                InitLists();
                GenerateVertexLayer(true);
                TrianglifyLayer(true);
                CreateTubeEndPlates();  
                break;
            case TubeType.Hollow:
                numVerts = RingPointCount * sampled.Count * 2;
                numTris = RingPointCount * numRings * 6 * 2;
                bool is360degree = TubeArc == 360.0f && MeshGenerator.IsTubeArcConstant;
                if (is360degree)
                    shouldDrawConnectingFace = true;
                else
                    shouldDrawConnectingFace = false;
                InitLists();
                GenerateVertexLayer(true);
                GenerateVertexLayer(false);
                TrianglifyLayer(true);
                TrianglifyLayer(false);
                if (!is360degree)
                    ConnectTubeInteriorAndExterior();
                ConnectTubeInteriorExteriorEnds();
                break;
        }
        IsBuzy = false;
    }
}
