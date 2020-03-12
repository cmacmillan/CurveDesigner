using Assets.NewUI;
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

    public static BezierCurve curve;

    public static IDistanceSampler<float> sizeDistanceSampler;

    public static int RingPointCount = 8;
    public static float Radius=3.0f;
    public static float VertexDensity=1.0f;
    public static float TubeArc = 360.0f;
    public static bool IsTubeArcConstant { get { return true; } }
    public static float Rotation = 0.0f;
    public static float Thickness = 0.0f;
    public static bool IsClosedLoop = false;
    public static CurveType CurveType;

    public static void StartGenerating(Curve3D curve)
    {
        if (!IsBuzy)
        {
            IsBuzy = true;
            BezierCurve clonedCurve = new BezierCurve(curve.positionCurve);
            lastUpdateTime = curve.lastMeshUpdateStartTime;

            MeshGenerator.curve = clonedCurve;
            MeshGenerator.RingPointCount = curve.ringPointCount;
            MeshGenerator.Radius = curve.curveRadius;
            MeshGenerator.VertexDensity = curve.curveVertexDensity;
            MeshGenerator.TubeArc = curve.arcOfTube;
            MeshGenerator.Rotation = curve.curveRotation;
            MeshGenerator.sizeDistanceSampler = new FloatLinearDistanceSampler(curve.sizeDistanceSampler);
            MeshGenerator.Thickness = curve.thickness;
            MeshGenerator.IsClosedLoop = curve.isClosedLoop;
            MeshGenerator.CurveType = curve.type;

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
        int ActualRingPointCount = RingPointCount - (TubeArc == 360.0 ? 1 : 0);
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
        void TrianglifyLayer(bool isExterior,int numPointsPerRing)
        {//generate tris
            int additionalRing = IsClosedLoop ? 1 : 0;
            int numVertsInALayer = (numRings+1) * numPointsPerRing;
            int basePoint = isExterior ? 0 :numVerts/2;
            for (int i = 0; i < numRings+additionalRing; i++)
            {
                int ringIndex = (i * numPointsPerRing)%numVertsInALayer;
                int nextRingIndex = (ringIndex + numPointsPerRing)%numVertsInALayer;
                for (int j = 0; j < numPointsPerRing; j++)
                {
                    if (!shouldDrawConnectingFace && (j + 1) >= numPointsPerRing)
                        continue;
                    if (isExterior)
                        DrawQuad(
                            ringIndex + j+basePoint,
                            ringIndex + ((j + 1) % numPointsPerRing)+basePoint,
                            nextRingIndex + j+basePoint,
                            nextRingIndex + ((j + 1) % numPointsPerRing)+basePoint);
                    else //flipped
                        DrawQuad(
                            ringIndex + ((j + 1) % numPointsPerRing)+basePoint,
                            ringIndex + j+basePoint,
                            nextRingIndex + ((j + 1) % numPointsPerRing)+basePoint,
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
            int additionalRing = IsClosedLoop ? 1 : 0;
            int numVertsInALayer = (numRings+1) * ActualRingPointCount;
            int interiorBase = numVerts / 2;
            for (int i = 0; i < numRings+additionalRing; i++)
            {
                int ringIndex = (i * ActualRingPointCount)%numVertsInALayer;
                int nextRingIndex = (ringIndex + ActualRingPointCount)%numVertsInALayer;
                DrawQuad(
                        ringIndex,
                        nextRingIndex,
                        interiorBase+ringIndex,
                        interiorBase+nextRingIndex
                    );
                DrawQuad(
                        nextRingIndex+ActualRingPointCount-1,
                        ringIndex+ActualRingPointCount-1,
                        interiorBase+nextRingIndex+ActualRingPointCount-1,
                        interiorBase+ringIndex+ActualRingPointCount-1
                    );
            }
        }
        void ConnectTubeInteriorExteriorEnds()
        {
            int interiorBase = numVerts / 2;
            //Then we gotta connect the ends as well
            int lastRingIndex = numRings * ActualRingPointCount;
            int firstRingIndex = 0;
            for (int j = 0; j < ActualRingPointCount; j++)
            {
                if (!shouldDrawConnectingFace && (j + 1) >= ActualRingPointCount)//will introduce a bug where curve never closes, even when angle is 360 TODO: revist
                    continue;
                DrawQuad(
                        firstRingIndex + ((j + 1) % ActualRingPointCount),
                        firstRingIndex+ j,
                        firstRingIndex + ((j + 1) % ActualRingPointCount) + interiorBase,
                        firstRingIndex + j + interiorBase
                    );
                DrawQuad(
                        lastRingIndex+ j,
                        lastRingIndex + ((j + 1) % ActualRingPointCount),
                        lastRingIndex + j + interiorBase,
                        lastRingIndex + ((j + 1) % ActualRingPointCount) + interiorBase
                    );
            }
        }
        void CreateTubeEndPlates()
        {
            //center point is average of ring
            int AddRingCenterVertexFromAverage(int baseIndex)
            {
                Vector3 average= Vector3.zero;
                for (int i = 0; i < ActualRingPointCount; i++)
                    average += vertices[i+baseIndex];
                average = average/ ActualRingPointCount;
                vertices.Add(average);
                return vertices.Count - 1;
            }
            int startRingBaseIndex = 0;
            int endRingBaseIndex = numRings* ActualRingPointCount;
            void TrianglifyRingToCenter(int baseIndex,int centerIndex,bool invert)
            {
                for (int i = 0; i < ActualRingPointCount; i++)
                {
                    if (invert)
                        DrawTri(
                            baseIndex + i,
                            centerIndex,
                            baseIndex + ((i + 1) % ActualRingPointCount)
                            );
                    else
                        DrawTri(
                            baseIndex + i,
                            baseIndex + ((i + 1) % ActualRingPointCount),
                            centerIndex
                            );
                }
            }
            TrianglifyRingToCenter(startRingBaseIndex, AddRingCenterVertexFromAverage(startRingBaseIndex),true);
            TrianglifyRingToCenter(endRingBaseIndex, AddRingCenterVertexFromAverage(endRingBaseIndex),false);
        }
        switch (CurveType)
        {
            case CurveType.Cylinder:
                numVerts = ActualRingPointCount * sampled.Count;
                numTris = ActualRingPointCount * numRings * 6;//each ring point except for the last ring has a quad (6) associated with it
                shouldDrawConnectingFace = true;
                InitLists();
                curve.CreateRingPointsAlongCurve(sampled, vertices, sizeDistanceSampler, TubeArc, Thickness, ActualRingPointCount, Rotation, true,IsClosedLoop,Radius);
                TrianglifyLayer(true,ActualRingPointCount);
                if (!IsClosedLoop)
                    CreateTubeEndPlates();
                break;
            case CurveType.HollowTube:
                numVerts = ActualRingPointCount * sampled.Count * 2;
                numTris = ActualRingPointCount * numRings * 6 * 2;
                bool is360degree = TubeArc == 360.0f && MeshGenerator.IsTubeArcConstant;
                if (is360degree)
                    shouldDrawConnectingFace = true;
                else
                    shouldDrawConnectingFace = false;
                InitLists();
                curve.CreateRingPointsAlongCurve(sampled, vertices, sizeDistanceSampler, TubeArc, Thickness, ActualRingPointCount, Rotation, true,IsClosedLoop,Radius);
                curve.CreateRingPointsAlongCurve(sampled, vertices, sizeDistanceSampler, TubeArc, Thickness, ActualRingPointCount, Rotation, false,IsClosedLoop,Radius);
                TrianglifyLayer(true,ActualRingPointCount);
                TrianglifyLayer(false,ActualRingPointCount);
                if (!is360degree)
                    ConnectTubeInteriorAndExterior();
                if (!IsClosedLoop)
                    ConnectTubeInteriorExteriorEnds();
                break;
            case CurveType.Flat:
                numVerts = 4 * sampled.Count;
                numTris = 8*(sampled.Count - 1);
                InitLists();
                curve.CreateRectanglePointsAlongCurve(sampled, vertices, Rotation, IsClosedLoop, Thickness, sizeDistanceSampler,Radius);
                shouldDrawConnectingFace = true;
                TrianglifyLayer(true,4);
                break;
        }
        IsBuzy = false;
    }
}
