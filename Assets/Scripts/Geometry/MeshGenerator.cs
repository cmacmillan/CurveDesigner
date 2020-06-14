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

    public static bool didMeshGenerationSucceed;
    public static List<Vector3> vertices;
    public static List<int> triangles;

    public static bool hasUVs;
    public static List<Vector2> uvs;

    public static bool IsBuzy = false;

    public static DateTime lastUpdateTime;

    public static BezierCurve curve;

    public static DoubleBezierSampler doubleBezierSampler;
    public static IDistanceSampler<float> sizeDistanceSampler;
    public static IDistanceSampler<float> rotationDistanceSampler;

    public static int doubleBezierSampleCount=20;
    public static int RingPointCount = 8;
    public static float Radius=3.0f;
    public static float VertexSampleDistance = 1.0f;
    public static float TubeArc = 360.0f;
    public static bool IsTubeArcConstant { get { return true; } }
    public static float Rotation = 0.0f;
    public static float Thickness = 0.0f;
    public static bool IsClosedLoop = false;
    public static CurveType CurveType;
    public static ThreadMesh meshToTile;
    public static float closeTilableMeshGap;
    public static MeshPrimaryAxis meshPrimaryAxis;

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
            MeshGenerator.doubleBezierSampler = new DoubleBezierSampler(curve.doubleBezierSampler);
            MeshGenerator.doubleBezierSampleCount = curve.doubleBezierSampleCount;
            MeshGenerator.Thickness = curve.thickness;
            MeshGenerator.IsClosedLoop = curve.isClosedLoop;
            MeshGenerator.CurveType = curve.type;
            MeshGenerator.meshToTile = curve.meshToTile == null ? null : new ThreadMesh(curve.meshToTile);
            MeshGenerator.closeTilableMeshGap = curve.closeTilableMeshGap;
            MeshGenerator.meshPrimaryAxis = curve.meshPrimaryAxis;

            Thread thread = new Thread(TryFinallyGenerateMesh);
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
    private static void TryFinallyGenerateMesh()
    {
        try
        {
            didMeshGenerationSucceed = false;
            if (GenerateMesh())
                didMeshGenerationSucceed = true;
        }
        finally
        {
            IsBuzy = false;
        }
    }
    private static bool GenerateMesh()
    {
        //Debug.Log("started thread");
        int numVerts;
        int numTris;
        bool shouldDrawConnectingFace;
        var sampled = curve.GetPointsWithSpacing(VertexSampleDistance);
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
        void InitLists(bool provideUvs = false)
        {
            InitOrClear(ref vertices);
            InitOrClear(ref triangles);
            if (provideUvs)
                InitOrClear(ref uvs);
            hasUVs = provideUvs;
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
        MeshIndexRing CreateRingPointsAlongCurveWithPrevious(MeshIndexRing previousRing, ref int pointIndexInList, PointOnCurve currentPoint, List<Vector3> listToAppend, IDistanceSampler<float> sizeSampler, float TubeArc, float TubeThickness, int RingPointCount, IDistanceSampler<float> rotationSampler, bool isExterior, bool isClosedLoop, float DefaultSize, float DefaultRotation, float curveLength)
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
                ring.points.Add(new MeshIndexRingPoint(theta, pointIndexInList));
                listToAppend.Add(currentPoint.GetRingPoint(theta, size));
                pointIndexInList++;
                if (i > minPreviousRingOverlapIndex)
                {
                    int currentPointIndex = listToAppend.Count - 1;
                    int previousPointIndex = currentPointIndex - 1;
                    int acrossIndex = previousRing.points[i].index;
                    int acrossPreviousIndex = previousRing.points[i - 1].index;//acrossIndex-1;//problem
                    DrawQuad(acrossPreviousIndex,acrossIndex, previousPointIndex,currentPointIndex);
                }
            }
            ///////////////////////////////////////////////START MIN////////////////////////////////////////////
            if (minPreviousRingOverlapIndex > 0)//remember gotta do the same for max
            {
                float theta = currentThetaMin;
                Vector3 rotatedVect = Quaternion.AngleAxis(theta, currentPoint.tangent) * currentPoint.reference;
                ring.points.Insert(0, new MeshIndexRingPoint(theta, pointIndexInList));
                listToAppend.Add(currentPoint.GetRingPoint(theta, size));
                pointIndexInList++;
                int currentPointIndex = ring.points[0].index;
                int previousPointIndex = ring.points[1].index;//out of range
                int crossIndex = previousRing.points[minPreviousRingOverlapIndex].index;
                int previousCrossIndex = previousRing.points[minPreviousRingOverlapIndex - 1].index;
                DrawQuad(previousPointIndex, currentPointIndex, crossIndex, previousCrossIndex);
            }
            //
            if (minPreviousRingOverlapIndex != int.MaxValue && minPreviousRingOverlapIndex > 1)//if there is some min overlap
            {
                int minSplitPointCount = minPreviousRingOverlapIndex - 1;
                int oldRingMinPointIndex = previousRing.points[0].index;
                int newRingMinPointIndex = ring.points[0].index;
                Vector3 startPos = listToAppend[newRingMinPointIndex];
                Vector3 totalDirectionVector = listToAppend[newRingMinPointIndex] - startPos;
                int splitStartIndex = listToAppend.Count;
                int newRingTipConnectPoint;
                //Then draw tip
                DrawTri(previousRing.points[0].index, previousRing.points[1].index, newRingMinPointIndex);
            }
            ////////////////////////////////////////////////END MIN/////////////////////////////////////////////
            ///////////////////////////////////////////////START MAX////////////////////////////////////////////
            if (maxPreviousRingOverlapIndex < previousRing.points.Count-1)//remember gotta do the same for max
            {
                float theta = currentThetaMax;
                Vector3 rotatedVect = Quaternion.AngleAxis(theta, currentPoint.tangent) * currentPoint.reference;
                ring.points.Add(new MeshIndexRingPoint(theta, pointIndexInList));
                listToAppend.Add(currentPoint.GetRingPoint(theta, size));
                pointIndexInList++;
                int currentPointIndex = ring.points[ring.points.Count-2].index;
                int previousPointIndex = ring.points[ring.points.Count-1].index;//out of range
                int crossIndex = previousRing.points[maxPreviousRingOverlapIndex].index;
                int previousCrossIndex = previousRing.points[maxPreviousRingOverlapIndex + 1].index;
                DrawQuad(previousPointIndex, currentPointIndex,previousCrossIndex,crossIndex);
            }
            //
            if (maxPreviousRingOverlapIndex != int.MinValue&& maxPreviousRingOverlapIndex<previousRing.points.Count-2)//if there is some min overlap
            {
                int minSplitPointCount =  - 1;
                int oldRingMinPointIndex = previousRing.points.Last().index;
                int newRingMinPointIndex = ring.points.Last().index;
                Vector3 startPos = listToAppend[newRingMinPointIndex];
                Vector3 totalDirectionVector = listToAppend[newRingMinPointIndex] - startPos;
                int splitStartIndex = listToAppend.Count;
                int newRingTipConnectPoint;
                //Then draw tip
                DrawTri(previousRing.points.Last(1).index, previousRing.points.Last().index, newRingMinPointIndex);
            }
            ////////////////////////////////////////////////END MAX/////////////////////////////////////////////
            return ring;
        }
        switch (CurveType)
        {
            case CurveType.Cylinder:
                {
                    numVerts = ActualRingPointCount * sampled.Count;
                    numTris = ActualRingPointCount * numRings * 6;//each ring point except for the last ring has a quad (6) associated with it
                    shouldDrawConnectingFace = true;
                    InitLists();
                    curve.CreateRingPointsAlongCurve(sampled, vertices, sizeDistanceSampler, TubeArc, Thickness, ActualRingPointCount, rotationDistanceSampler, true, IsClosedLoop, Radius, Rotation, curve.GetLength());
                    TrianglifyLayer(true, ActualRingPointCount);
                    if (!IsClosedLoop)
                        CreateTubeEndPlates();
                    return true;
                }
            case CurveType.HollowTube:
                #region hollowtube
                {
                    numVerts = ActualRingPointCount * sampled.Count * 2;
                    numTris = ActualRingPointCount * numRings * 6 * 2;
                    InitLists();
                    bool is360degree = TubeArc == 360.0f && MeshGenerator.IsTubeArcConstant;
                    if (is360degree)
                        shouldDrawConnectingFace = true;
                    else
                        shouldDrawConnectingFace = false;
                    InitLists();
                    curve.CreateRingPointsAlongCurve(sampled, vertices, sizeDistanceSampler, TubeArc, Thickness, ActualRingPointCount, rotationDistanceSampler, true, IsClosedLoop, Radius, Rotation, curve.GetLength());
                    curve.CreateRingPointsAlongCurve(sampled, vertices, sizeDistanceSampler, TubeArc, Thickness, ActualRingPointCount, rotationDistanceSampler, false, IsClosedLoop, Radius, Rotation, curve.GetLength());
                    TrianglifyLayer(true, ActualRingPointCount);
                    TrianglifyLayer(false, ActualRingPointCount);
                    if (!is360degree)
                        ConnectTubeInteriorAndExterior();
                    if (!IsClosedLoop)
                        ConnectTubeInteriorExteriorEnds();
                    return true;
                }
            #endregion
            case CurveType.Flat:
                #region flat
                {
                    numVerts = 4 * sampled.Count;
                    numTris = 8 * (sampled.Count - 1) + 4;
                    InitLists();
                    curve.CreateRectanglePointsAlongCurve(sampled, vertices, rotationDistanceSampler, IsClosedLoop, Thickness, sizeDistanceSampler, Radius, Rotation, curve.GetLength());
                    shouldDrawConnectingFace = true;
                    TrianglifyLayer(true, 4);
                    CreateFlatEndPlates();
                    return true;
                }
            #endregion
            case CurveType.DoubleBezier:
                {
                    InitLists();
                    var primaryCurveSamples = curve.GetPointsWithSpacing(VertexSampleDistance);
                    List<Vector3> backSideBuffer = new List<Vector3>();
                    doubleBezierSampler.CacheOpenCurvePoints(curve);
                    int triangleIndex = 0;
                    doubleBezierSampler.SampleAt(0, 0, curve, out Vector3 prev);
                    foreach (var primaryCurvePoint in primaryCurveSamples)
                    {
                        int flip;
                        {
                            doubleBezierSampler.SampleAt(primaryCurvePoint.distanceFromStartOfCurve, 0, curve, out Vector3 reference);
                            flip = Vector3.Dot(reference, Vector3.right) < 0 ? -1 : 1 ;
                            prev = reference;
                        }
                        for (float c = 0; c <= doubleBezierSampleCount; c++)
                        {
                            float progress = c / (float)doubleBezierSampleCount;
                            var relativePos = doubleBezierSampler.SampleAt(primaryCurvePoint.distanceFromStartOfCurve, progress, curve,out Vector3 reference);
                            //Lets say z is forward
                            var cross = Vector3.Cross(primaryCurvePoint.tangent, primaryCurvePoint.reference);
                            Vector3 TransformVector3(Vector3 vect)
                            {
                                return -cross * vect.x + primaryCurvePoint.reference * vect.y +primaryCurvePoint.tangent * vect.z;
                            }
                            var absolutePos = primaryCurvePoint.position +TransformVector3(relativePos);
                            vertices.Add(absolutePos+flip*TransformVector3(reference)*Thickness/2);
                            backSideBuffer.Add(absolutePos-flip*TransformVector3(reference)*Thickness/2);
                        }
                    }
                    vertices.AddRange(backSideBuffer);
                    numVerts = vertices.Count;
                    shouldDrawConnectingFace = false;
                    numRings =  primaryCurveSamples.Count - 1;//Minus 1?
                    TrianglifyLayer(true, doubleBezierSampleCount+1);
                    TrianglifyLayer(false, doubleBezierSampleCount+1);
                    return true;
                }
            case CurveType.Mesh:
                {
                    InitLists(true);
                    //we are gonna assume that the largest dimension of the bounding box is the correct direction, and that the mesh is axis aligned and it is perpendicular to the edge of the bounding box
                    var bounds = meshToTile.bounds;
                    //watch out for square meshes
                    float meshLength=-1;
                    float secondaryDimensionLength=-1;
                    {
                        Quaternion rotation=Quaternion.identity;
                        void UseXAsMainAxis()
                        {
                            meshLength = bounds.extents.x * 2;
                            secondaryDimensionLength = Mathf.Max(bounds.extents.y,bounds.extents.z);
                            rotation = Quaternion.FromToRotation(Vector3.right, Vector3.right);//does nothing
                        }
                        void UseYAsMainAxis()
                        {
                            meshLength = bounds.extents.y * 2;
                            secondaryDimensionLength = Mathf.Max(bounds.extents.x,bounds.extents.z);
                            rotation = Quaternion.FromToRotation(Vector3.up, Vector3.right);
                        }
                        void UseZAsMainAxis()
                        {
                            meshLength = bounds.extents.z * 2;
                            secondaryDimensionLength = Mathf.Max(bounds.extents.x,bounds.extents.y);
                            rotation = Quaternion.FromToRotation(Vector3.forward, Vector3.right);
                        }
                        switch (meshPrimaryAxis)
                        {
                            case MeshPrimaryAxis.auto:
                                if ((bounds.extents.x >= bounds.extents.y && bounds.extents.x >= bounds.extents.z))
                                    UseXAsMainAxis();
                                else if ((bounds.extents.y >= bounds.extents.x && bounds.extents.y >= bounds.extents.z))
                                    UseYAsMainAxis();
                                else
                                    UseZAsMainAxis();
                                break;
                            case MeshPrimaryAxis.x:
                                UseXAsMainAxis();
                                break;
                            case MeshPrimaryAxis.y:
                                UseYAsMainAxis();
                                break;
                            case MeshPrimaryAxis.z:
                                UseZAsMainAxis();
                                break;
                        }
                        Vector3 TransformPoint(Vector3 point)
                        {
                            return (rotation * (point - bounds.center)) + new Vector3(meshLength / 2, 0, 0);
                        }
                        for (int i = 0; i < meshToTile.verts.Length; i++)
                        {
                            meshToTile.verts[i] = TransformPoint(meshToTile.verts[i]);
                            meshToTile.verts[i].x = Mathf.Max(0, meshToTile.verts[i].x);//clamp above zero, sometimes floats mess with this
                        }
                        //now x is always along the mesh and normalized around the center
                    }
                    var curveLength = curve.GetLength();
                    int vertCount = meshToTile.verts.Length;
                    bool useUvs = meshToTile.uv.Length ==meshToTile.verts.Length;
                    float GetSize(float dist)
                    {
                        return sizeDistanceSampler.GetValueAtDistance(dist, IsClosedLoop, curveLength, curve) + Radius;
                    }
                    float GetDistanceByArea(float area)
                    {
                        return sizeDistanceSampler.GetDistanceByAreaUnderInverseCurve(area, IsClosedLoop, curveLength, curve,Radius);
                    }
                    int c = 0;
                    int vertexBaseOffset = 0;
                    List<int> remappedVerts = new List<int>();
                    for (float f = 0; f < curveLength;)
                    {
                        float max = float.MinValue;
                        remappedVerts.Clear();
                        int skippedVerts = 0;
                        for (int i = 0; i < meshToTile.verts.Length; i++)
                        {
                            var vert = meshToTile.verts[i];
                            var distance = GetDistanceByArea((vert.x + c * (closeTilableMeshGap + meshLength))/secondaryDimensionLength);
                            max = Mathf.Max(max, distance);
                            if (distance > curveLength)
                            {
                                remappedVerts.Add(-1);
                                skippedVerts++;
                                continue;
                            }
                            else
                            {
                                remappedVerts.Add(i-skippedVerts);
                            }
                            var point = curve.GetPointAtDistance(distance);
                            var rotation = rotationDistanceSampler.GetValueAtDistance(distance, IsClosedLoop, curveLength, curve) + Rotation;
                            var size = GetSize(distance);
                            var sizeScale = size / secondaryDimensionLength;
                            var reference = Quaternion.AngleAxis(rotation, point.tangent) * point.reference;
                            var cross = Vector3.Cross(reference, point.tangent);
                            vertices.Add(point.position + reference * vert.y * sizeScale + cross * vert.z * sizeScale);
                            if (useUvs)
                                uvs.Add(meshToTile.uv[i]);
                        }
                        for (int i = 0; i < meshToTile.tris.Length; i+=3)
                        {
                            var tri1 = meshToTile.tris[i];
                            var tri2 = meshToTile.tris[i+1];
                            var tri3 = meshToTile.tris[i+2];
                            int remappedTri1 = remappedVerts[tri1];
                            int remappedTri2 = remappedVerts[tri2];
                            int remappedTri3 = remappedVerts[tri3];
                            if (remappedTri1 == -1 || remappedTri2 == -1 || remappedTri3 == -1)
                                continue;
                            triangles.Add(remappedTri1+vertexBaseOffset);
                            triangles.Add(remappedTri2+vertexBaseOffset);
                            triangles.Add(remappedTri3+vertexBaseOffset);
                        }
                        vertexBaseOffset += vertCount - skippedVerts;
                        c++;
                        f = max;
                    }
                    if (vertexBaseOffset >= 65535)
                    {
                        Debug.LogError("Too many verticies, unable to correctly model mesh");
                        return false;
                    }
                    ///end temp
                    for (int i = 0; i < triangles.Count; i += 3)
                    {
                        var swap = triangles[i];
                        triangles[i] = triangles[i + 2];
                        triangles[i + 2] = swap;
                    }
                    return true;
                }
            default:
                return false;
        }
    }
}
public static class ListExtensionMethods
{
    public static T Last<T>(this List<T> lst,int indexFromLast=0)
    {
        return lst[lst.Count - 1-indexFromLast];
    }
}
