using Assets.NewUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public static class MeshGenerator
{
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
    public static List<Vector2> uvs;

    public static bool hasUVs;

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
    private struct EdgePointInfo
    {
        public float distanceAlongCurve;
        public Vector3 side1Point1;
        public Vector3 side1Point2;
        public Vector3 side2Point1;
        public Vector3 side2Point2;
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
        float curveLength = curve.GetLength();
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
        void TrianglifyLayer(bool isExterior, int numPointsPerRing)
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
        void CreateEdgeVertsTrisAndUvs(List<EdgePointInfo> edgePointInfos)
        {
            int firsts1p1=-1;
            int firsts1p2=-1;
            int firsts2p1=-1;
            int firsts2p2=-1;

            int prevs1p1=-1;
            int prevs1p2=-1;
            int prevs2p1=-1;
            int prevs2p2=-1;

            int s1p1 = -1;
            int s1p2 = -1;
            int s2p1 = -1;
            int s2p2 = -1;
            void DoTris()
            {
                DrawQuad(s1p1, s1p2,prevs1p1, prevs1p2);
                DrawQuad(prevs2p1, prevs2p2, s2p1, s2p2);
            }

            for (int i=0;i<edgePointInfos.Count;i++)
            {
                var curr = edgePointInfos[i];
                vertices.Add(curr.side1Point1);
                vertices.Add(curr.side1Point2);
                vertices.Add(curr.side2Point1);
                vertices.Add(curr.side2Point2);

                var uvX = curr.distanceAlongCurve / curveLength;

                uvs.Add(new Vector2(uvX,0.0f));
                uvs.Add(new Vector2(uvX,1.0f));
                uvs.Add(new Vector2(uvX,0.0f));
                uvs.Add(new Vector2(uvX,1.0f));

                s1p1 = vertices.Count - 4;
                s1p2 = vertices.Count - 3;
                s2p1 = vertices.Count - 2;
                s2p2 = vertices.Count - 1;
                if (i > 0)
                {
                    DoTris();
                }
                else
                {
                    firsts1p1 = s1p1;
                    firsts1p2 = s1p2;
                    firsts2p1 = s2p1;
                    firsts2p2 = s2p2;
                }
                prevs1p1 = s1p1;
                prevs1p2 = s1p2;
                prevs2p1 = s2p1;
                prevs2p2 = s2p2;
            }
            if (IsClosedLoop)
            {
                s1p1 = firsts1p1;
                s1p2 = firsts1p2;
                s2p1 = firsts1p2;
                s2p2 = firsts2p2;
                DoTris();
            }
        }
        void CreateRingPointsAlongCurve(BezierCurve curve, List<PointOnCurve> points, IDistanceSampler<float> sizeSampler, float TubeArc, float TubeThickness, int RingPointCount, IDistanceSampler<float> rotationSampler, bool isExterior, bool isClosedLoop, float DefaultSize, float DefaultRotation)
        {
            float distanceFromFull = 360.0f - TubeArc;
            for (int i = 0; i < points.Count; i++)
            {
                PointOnCurve currentPoint = points[i];
                float offset = (isExterior ? .5f : -.5f) * (TubeThickness);
                var size = Mathf.Max(0, sizeSampler.GetValueAtDistance(currentPoint.distanceFromStartOfCurve, isClosedLoop, curveLength, curve) + offset + DefaultSize);
                var rotation = rotationSampler.GetValueAtDistance(currentPoint.distanceFromStartOfCurve, isClosedLoop, curveLength, curve) + DefaultRotation;
                for (int j = 0; j < RingPointCount; j++)
                {
                    float theta = (TubeArc * j / (RingPointCount - (TubeArc == 360.0 ? 0 : 1))) + distanceFromFull / 2 + rotation;
                    Vector3 rotatedVect = Quaternion.AngleAxis(theta, currentPoint.tangent) * currentPoint.reference;
                    vertices.Add(currentPoint.GetRingPoint(theta, size));
                }
            }
        }
        void CreateRectanglePointsAlongCurve(List<PointOnCurve> points, List<EdgePointInfo> edgePointInfos)
        {
            List<Vector3> vertBuffer = new List<Vector3>();
            List<Vector2> uvBuffer = new List<Vector2>();
            for (int i = 0; i < points.Count; i++)
            {
                PointOnCurve currentPoint = points[i];
                float uvx = currentPoint.distanceFromStartOfCurve / curveLength;
                var center = currentPoint.position;
                var rotation = rotationDistanceSampler.GetValueAtDistance(currentPoint.distanceFromStartOfCurve, IsClosedLoop, curveLength, curve) + Rotation;
                var up = Quaternion.AngleAxis(rotation, currentPoint.tangent) * currentPoint.reference.normalized;
                var right = Vector3.Cross(up, currentPoint.tangent).normalized;
                var scaledUp = up * Thickness / 2.0f;
                var scaledRight = right * Mathf.Max(0, sizeDistanceSampler.GetValueAtDistance(currentPoint.distanceFromStartOfCurve, IsClosedLoop, curveLength, curve) + Radius);

                var side1point1 = center + scaledUp + scaledRight;
                var side2point1 = center + scaledUp - scaledRight;
                var side1point2 = center - scaledUp + scaledRight;
                var side2point2 = center - scaledUp - scaledRight;

                vertices.Add(side1point1);
                vertices.Add(side2point1);
                vertBuffer.Add(side1point2);
                vertBuffer.Add(side2point2);

                uvs.Add(new Vector2(uvx, 0));
                uvs.Add(new Vector2(uvx, 1));
                uvBuffer.Add(new Vector2(uvx, 0));
                uvBuffer.Add(new Vector2(uvx, 1));

                edgePointInfos.Add(new EdgePointInfo() {
                    distanceAlongCurve = currentPoint.distanceFromStartOfCurve,
                    side1Point1 = side1point1,
                    side1Point2 = side1point2,
                    side2Point1 = side2point1,
                    side2Point2 = side2point2
                });
            }
            vertices.AddRange(vertBuffer);
            uvs.AddRange(uvBuffer);
        }
        void InitLists(bool provideUvs = false)
        {
            InitOrClear(ref vertices);
            InitOrClear(ref triangles);
            if (provideUvs)
                InitOrClear(ref uvs);
            hasUVs = provideUvs;
        }
        void ConnectMeshInteriorAndExteriorLayers()
        {
            int additionalRing = IsClosedLoop ? 1 : 0;
            int numVertsInALayer = (numRings + 1) * ActualRingPointCount;
            int interiorBase = numVerts / 2;
            for (int i = 0; i < numRings + additionalRing; i++)
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
        void CreateMeshInteriorExteriorEndPlates()
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
        switch (CurveType)
        {
            case CurveType.Cylinder:
                {
                    numVerts = ActualRingPointCount * sampled.Count;
                    numTris = ActualRingPointCount * numRings * 6;//each ring point except for the last ring has a quad (6) associated with it
                    shouldDrawConnectingFace = true;
                    InitLists();
                    CreateRingPointsAlongCurve(curve,sampled, sizeDistanceSampler, TubeArc, Thickness, ActualRingPointCount, rotationDistanceSampler, true, IsClosedLoop, Radius, Rotation);
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
                    CreateRingPointsAlongCurve(curve,sampled, sizeDistanceSampler, TubeArc, Thickness, ActualRingPointCount, rotationDistanceSampler, true, IsClosedLoop, Radius, Rotation);
                    CreateRingPointsAlongCurve(curve,sampled, sizeDistanceSampler, TubeArc, Thickness, ActualRingPointCount, rotationDistanceSampler, false, IsClosedLoop, Radius, Rotation);
                    TrianglifyLayer(true, ActualRingPointCount);
                    TrianglifyLayer(false, ActualRingPointCount);
                    if (!is360degree)
                        ConnectMeshInteriorAndExteriorLayers();
                    if (!IsClosedLoop)
                        CreateMeshInteriorExteriorEndPlates();
                    return true;
                }
            #endregion
            case CurveType.Flat:
                #region flat
                {
                    numVerts = 4 * sampled.Count;
                    numTris = 8 * (sampled.Count - 1) + 4;
                    InitLists(true);
                    List<EdgePointInfo> edgePointInfos = new List<EdgePointInfo>();
                    CreateRectanglePointsAlongCurve(sampled,edgePointInfos);
                    shouldDrawConnectingFace = false;//true;
                    TrianglifyLayer(false, 2);
                    TrianglifyLayer(true, 2);
                    CreateEdgeVertsTrisAndUvs(edgePointInfos);
                    //CreateFlatEndPlates();
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
                    foreach (var primaryCurvePoint in primaryCurveSamples)
                    {
                        for (float c = 0; c <= doubleBezierSampleCount; c++)
                        {
                            float progress = c / (float)doubleBezierSampleCount;
                            var relativePos = doubleBezierSampler.SampleAt(primaryCurvePoint.distanceFromStartOfCurve, progress, curve,out Vector3 reference);
                            //Lets say z is forward
                            var cross = Vector3.Cross(primaryCurvePoint.tangent, primaryCurvePoint.reference).normalized;
                            Vector3 TransformVector3(Vector3 vect)
                            {
                                return (Quaternion.LookRotation(primaryCurvePoint.tangent,primaryCurvePoint.reference)*vect);
                            }
                            var absolutePos = primaryCurvePoint.position +TransformVector3(relativePos);
                            vertices.Add(absolutePos+TransformVector3(reference)*Thickness/2);
                            backSideBuffer.Add(absolutePos-TransformVector3(reference)*Thickness/2);
                        }
                    }
                    vertices.AddRange(backSideBuffer);
                    numVerts = vertices.Count;
                    shouldDrawConnectingFace = false;
                    numRings =  primaryCurveSamples.Count - 1;//Minus 1?
                    ActualRingPointCount = doubleBezierSampleCount+1;
                    TrianglifyLayer(true, ActualRingPointCount);
                    TrianglifyLayer(false, ActualRingPointCount);
                    ConnectMeshInteriorAndExteriorLayers();
                    if (!IsClosedLoop)
                        CreateMeshInteriorExteriorEndPlates();
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
