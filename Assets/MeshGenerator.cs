using Assets.NewUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public static class MeshGenerator
{
    public class MeshIndexRingPoint
    {
        public MeshIndexRingPoint(float angle, int index)
        {
            this.angle = angle;
            this.index = index;
        }
        public float angle;
        public int index;
    }
    public class MeshIndexRing
    {
        public float minTheta;
        public float maxTheta;
        public List<MeshIndexRingPoint> points = new List<MeshIndexRingPoint>();
    }
    public static List<Vector3> vertices;
    public static List<int> triangles;

    public static bool IsBuzy = false;

    public static DateTime lastUpdateTime;

    public static BezierCurve curve;

    public static IDistanceSampler<float> sizeDistanceSampler;
    public static IDistanceSampler<float> rotationDistanceSampler;

    public static int RingPointCount = 8;
    public static float Radius=3.0f;
    public static float VertexSampleDistance = 1.0f;
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
            MeshGenerator.VertexSampleDistance = curve.GetVertexDensityDistance();
            MeshGenerator.TubeArc = curve.arcOfTube;
            MeshGenerator.Rotation = curve.curveRotation;
            MeshGenerator.sizeDistanceSampler = new FloatLinearDistanceSampler(curve.sizeDistanceSampler,clonedCurve);
            MeshGenerator.rotationDistanceSampler = new FloatLinearDistanceSampler(curve.rotationDistanceSampler,clonedCurve);
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
        var sampled = curve.GetPointsWithSpacing(VertexSampleDistance);
        int numRings = sampled.Count - 1;
        int ActualRingPointCount = RingPointCount - (TubeArc == 360.0 ? 1 : 0);
        void DrawQuad(int side1Point1, int side1Point2, int side2Point1, int side2Point2,bool draw=false)
        {
            if (!draw)
                return;
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
        //var rand = new System.Random();
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
                            ringIndex + j + basePoint,
                            ringIndex + ((j + 1) % numPointsPerRing) + basePoint,
                            nextRingIndex + j + basePoint,
                            nextRingIndex + ((j + 1) % numPointsPerRing) + basePoint);
                    else //flipped
                        DrawQuad(
                            ringIndex + ((j + 1) % numPointsPerRing) + basePoint,
                            ringIndex + j + basePoint,
                            nextRingIndex + ((j + 1) % numPointsPerRing) + basePoint,
                            nextRingIndex + j + basePoint);
                }
            }
        }
        void InitLists()
        {
            InitOrClear(ref vertices);
            InitOrClear(ref triangles);
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
        void CreateFlatEndPlates()
        {
            DrawQuad(1,0,2,3);
            var end = numVerts - 4;
            DrawQuad(end, end+1, end + 3, end + 2);
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
        MeshIndexRing CreateRingPointsAlongCurveWithPrevious(MeshIndexRing previousRing, ref int pointIndex, PointOnCurve currentPoint, List<Vector3> listToAppend, IDistanceSampler<float> sizeSampler, float TubeArc, float TubeThickness, int RingPointCount, IDistanceSampler<float> rotationSampler, bool isExterior, bool isClosedLoop, float DefaultSize, float DefaultRotation, float curveLength)
        {
            var ring = new MeshIndexRing();
            var rotation = rotationSampler.GetValueAtDistance(currentPoint.distanceFromStartOfCurve, isClosedLoop, curveLength, curve) + DefaultRotation;
            float distanceFromFull = 360.0f - TubeArc;
            float currentThetaMin = distanceFromFull / 2 + rotation;
            float currentThetaMax = TubeArc + currentThetaMin;
            float offset = (isExterior ? .5f : -.5f) * (TubeThickness);
            var size = Mathf.Max(0, sizeSampler.GetValueAtDistance(currentPoint.distanceFromStartOfCurve, isClosedLoop, curveLength, curve) + offset + DefaultSize);

            int minPreviousRingOverlapIndex = int.MaxValue;//The smallest index from previous ring that is within both rings theta
            for (int i = 0; i < previousRing.points.Count; i++)
                if (previousRing.points[i].angle >= currentThetaMin)
                {
                    minPreviousRingOverlapIndex = i;
                    break;
                }

            int maxPreviousRingOverlapIndex = int.MinValue;
            for (int i = previousRing.points.Count - 1; i >= 0; i--)
                if (previousRing.points[i].angle <= currentThetaMax)
                {
                    maxPreviousRingOverlapIndex = i;
                    break;
                }

            ring.minTheta = currentThetaMin;
            ring.maxTheta = currentThetaMax;

            for (int i = minPreviousRingOverlapIndex; i <= maxPreviousRingOverlapIndex; i++)
            {
                float theta = previousRing.points[i].angle;
                Vector3 rotatedVect = Quaternion.AngleAxis(theta, currentPoint.tangent) * currentPoint.reference;
                ring.points.Add(new MeshIndexRingPoint(theta, pointIndex));
                listToAppend.Add(currentPoint.GetRingPoint(theta, size));
                pointIndex++;
                if (i > minPreviousRingOverlapIndex)
                {
                    int currentPointIndex = listToAppend.Count - 1;
                    int previousPointIndex = currentPointIndex - 1;
                    int acrossIndex = previousRing.points[i].index;
                    int acrossPreviousIndex = acrossIndex-1;
                    DrawQuad(acrossPreviousIndex,acrossIndex, previousPointIndex,currentPointIndex,true);
                }
            }
            if (minPreviousRingOverlapIndex > 0)//remember gotta do the same for max
            {
                float theta = currentThetaMin;
                Vector3 rotatedVect = Quaternion.AngleAxis(theta, currentPoint.tangent) * currentPoint.reference;
                ring.points.Insert(0,new MeshIndexRingPoint(theta, pointIndex));
                listToAppend.Add(currentPoint.GetRingPoint(theta, size));   
                pointIndex++;
                int currentPointIndex = ring.points[0].index;
                int previousPointIndex = ring.points[1].index;
                int crossIndex = previousRing.points[minPreviousRingOverlapIndex].index;
                int previousCrossIndex =previousRing.points[minPreviousRingOverlapIndex-1].index;
                DrawQuad(previousPointIndex,currentPointIndex,crossIndex,previousCrossIndex,false);
            }
            //
            if (minPreviousRingOverlapIndex != int.MaxValue && minPreviousRingOverlapIndex>1)//if there is some min overlap
            {
                int minSplitPointCount = minPreviousRingOverlapIndex - 1;
                int oldRingMinPointIndex=previousRing.points[0].index;
                int newRingMinPointIndex=ring.points[0].index;
                Vector3 startPos = listToAppend[newRingMinPointIndex];
                Vector3 totalDirectionVector = listToAppend[newRingMinPointIndex]-startPos;
                int splitStartIndex = listToAppend.Count;
                int newRingTipConnectPoint;
                /*
                if (minSplitPointCount == 0)
                    newRingTipConnectPoint = newRingMinPointIndex;
                else
                {
                    int previousNewRingPoint = newRingMinPointIndex;
                    int previousOldRingPoint = oldRingMinPointIndex;
                    for (int i = 1; i <= minSplitPointCount; i++)
                    {
                        listToAppend.Add(startPos + totalDirectionVector * i / (float)(minSplitPointCount + 1));
                        int currNewRingPointIndex = listToAppend.Count - 1;
                        int currOldRingPointIndex = ring.points[minSplitPointCount - i].index;
                        DrawQuad(currNewRingPointIndex,previousNewRingPoint,currOldRingPointIndex,previousOldRingPoint);
                    }
                    newRingTipConnectPoint = listToAppend.Count - 1;
                }
                */
                //Then draw tip
                //we should only draw tip if the previous ring produced an excess point
                //if ()
                DrawTri(previousRing.points[0].index,previousRing.points[1].index,newRingMinPointIndex);
            }
            //
            return ring;
        }
        switch (CurveType)
        {
            case CurveType.Cylinder:
                numVerts = ActualRingPointCount * sampled.Count;
                numTris = ActualRingPointCount * numRings * 6;//each ring point except for the last ring has a quad (6) associated with it
                shouldDrawConnectingFace = true;
                InitLists();
                curve.CreateRingPointsAlongCurve(sampled, vertices, sizeDistanceSampler, TubeArc, Thickness, ActualRingPointCount, rotationDistanceSampler, true, IsClosedLoop, Radius, Rotation, curve.GetLength());
                TrianglifyLayer(true, ActualRingPointCount);
                if (!IsClosedLoop)
                    CreateTubeEndPlates();
                break;
            case CurveType.HollowTube:
                InitLists();
                int pointIndex = 0;
                var previousRing = curve.CreateRingPointsAlongCurve(ref pointIndex, sampled[0], vertices, sizeDistanceSampler, TubeArc, Thickness, ActualRingPointCount, rotationDistanceSampler, true, IsClosedLoop, Radius, Rotation, curve.GetLength());
                for (int i = 0; i < sampled.Count; i++)
                    previousRing = CreateRingPointsAlongCurveWithPrevious(previousRing,ref pointIndex, sampled[i], vertices, sizeDistanceSampler, TubeArc, Thickness, ActualRingPointCount, rotationDistanceSampler, true, IsClosedLoop, Radius, Rotation, curve.GetLength());
                //NewTrianglifyLayer();
                break; 
                /*numVerts = ActualRingPointCount * sampled.Count * 2;
                numTris = ActualRingPointCount * numRings * 6 * 2;
                bool is360degree = TubeArc == 360.0f && MeshGenerator.IsTubeArcConstant;
                if (is360degree)
                    shouldDrawConnectingFace = true;
                else
                    shouldDrawConnectingFace = false;
                InitLists();
                curve.CreateRingPointsAlongCurve(sampled, vertices, sizeDistanceSampler, TubeArc, Thickness, ActualRingPointCount, rotationDistanceSampler, true,IsClosedLoop,Radius,Rotation,curve.GetLength());
                curve.CreateRingPointsAlongCurve(sampled, vertices, sizeDistanceSampler, TubeArc, Thickness, ActualRingPointCount, rotationDistanceSampler, false,IsClosedLoop,Radius,Rotation,curve.GetLength());
                TrianglifyLayer(true,ActualRingPointCount);
                TrianglifyLayer(false,ActualRingPointCount);
                if (!is360degree)
                    ConnectTubeInteriorAndExterior();
                if (!IsClosedLoop)
                    ConnectTubeInteriorExteriorEnds();
                break;*/
            case CurveType.Flat:
                InitLists();
                float curveLength = curve.GetLength();

                Vector3 previousUpRight=Vector3.zero;
                int previousUpRightIndex = -1;

                Vector3 previousUpLeft=Vector3.zero;
                int previousUpLeftIndex = -1;

                Vector3 previousDownRight=Vector3.zero;
                int previousDownRightIndex = -1;

                Vector3 previousDownLeft=Vector3.zero;
                int previousDownLeftIndex = -1;


                for (int i=0;i<sampled.Count;i++)
                {
                    PointOnCurve currentPoint = sampled[i];
                    var center = currentPoint.position;
                    var rotation = rotationDistanceSampler.GetValueAtDistance(currentPoint.distanceFromStartOfCurve, IsClosedLoop, curveLength,curve) + Rotation;
                    var up = Quaternion.AngleAxis(rotation, currentPoint.tangent) * currentPoint.reference.normalized;
                    var right = Vector3.Cross(up, currentPoint.tangent).normalized;
                    var scaledUp = up * Thickness / 2.0f;
                    var scaledRight = right * Mathf.Max(0, sizeDistanceSampler.GetValueAtDistance(currentPoint.distanceFromStartOfCurve, IsClosedLoop, curveLength,curve) + Radius);

                    var upRight = center + scaledUp + scaledRight;
                    var upLeft = center + scaledUp - scaledRight;
                    var downLeft = center - scaledUp - scaledRight;
                    var downRight = center - scaledUp + scaledRight;

                    int baseIndex = vertices.Count;

                    int upRightIndex = baseIndex;
                    vertices.Add(upRight);

                    int upLeftIndex = baseIndex + 1;
                    vertices.Add(upLeft);

                    int downLeftIndex = baseIndex + 2;
                    vertices.Add(downLeft);

                    int downRightIndex = baseIndex + 3;
                    vertices.Add(downRight);


                    if (i > 0)
                    {
                        Vector3 topAverage = (previousUpLeft + previousUpRight + upLeft + upRight) / 4.0f;
                        Vector3 bottomAverage = (previousDownLeft + previousDownRight + downLeft + downRight) / 4.0f;

                        int topAverageIndex = baseIndex + 4;
                        vertices.Add(topAverage);

                        int bottomAverageIndex = baseIndex + 5;
                        vertices.Add(bottomAverage);

                        if (true)
                        {
                            //top
                            DrawTri(previousUpRightIndex, previousUpLeftIndex, topAverageIndex);
                            DrawTri(previousUpLeftIndex, upLeftIndex, topAverageIndex);
                            DrawTri(upLeftIndex, upRightIndex, topAverageIndex);
                            DrawTri(upRightIndex, previousUpRightIndex, topAverageIndex);

                            //bottom
                            DrawTri(previousDownLeftIndex, previousDownRightIndex, bottomAverageIndex);
                            DrawTri(downLeftIndex, previousDownLeftIndex, bottomAverageIndex);
                            DrawTri(downRightIndex, downLeftIndex, bottomAverageIndex);
                            DrawTri(previousDownRightIndex, downRightIndex, bottomAverageIndex);
                        }
                        else
                        {
                            DrawQuad(previousUpRightIndex,previousUpLeftIndex,upRightIndex,upLeftIndex);
                        }
                    }
                    /*else
                    {
                        DrawQuad(previousUpRightIndex,previousUpLeftIndex,upLeftIndex,upRightIndex);
                        DrawQuad(previousDownLeftIndex, previousDownRightIndex, downRightIndex, downLeftIndex);
                    }*/

                    previousUpRight = upRight;
                    previousUpRightIndex = upRightIndex;

                    previousUpLeft = upLeft;
                    previousUpLeftIndex = upLeftIndex;

                    previousDownRight = downRight;
                    previousDownRightIndex = downRightIndex;

                    previousDownLeft = downLeft;
                    previousDownLeftIndex = downLeftIndex;
                }
                break;
                /*
                 * numVerts = 4 * sampled.Count;
                numTris = 8*(sampled.Count - 1)+4;
                curve.CreateRectanglePointsAlongCurve(sampled, vertices, rotationDistanceSampler, IsClosedLoop, Thickness, sizeDistanceSampler,Radius,Rotation,curve.GetLength());
                shouldDrawConnectingFace = true;
                TrianglifyLayer(true,4);
                CreateFlatEndPlates();
                break;
                */
        }
        IsBuzy = false;
    }
}
