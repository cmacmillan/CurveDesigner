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
    public static List<Color32> colors;

    public static bool IsBuzy = false;

    public static DateTime lastUpdateTime;

    public static BezierCurve curve;

    //public static Color32[] displacementColors;

    public static DoubleBezierSampler doubleBezierSampler;
    public static FloatDistanceSampler sizeSampler;
    public static FloatDistanceSampler rotationSampler;
    public static ColorDistanceSampler colorSampler;
    public static FloatDistanceSampler tubeArcSampler;
    public static FloatDistanceSampler thicknessSampler;

    public static int RingPointCount = 8;
    public static float VertexSampleDistance = 1.0f;

    public static bool clampAndStretchMeshToCurve = true;
    public static bool IsClosedLoop = false;
    public static CurveType CurveType;
    public static ThreadMesh meshToTile;
    public static float closeTilableMeshGap;
    public static MeshPrimaryAxis meshPrimaryAxis;

    private struct EdgePointInfo
    {
        public float distanceAlongCurve;
        public Vector3 side1Point1;
        public Vector3 side1Point2;
        public Vector3 side2Point1;
        public Vector3 side2Point2;
    }

    public static void StartGenerating(Curve3D curve)
    {
        if (!IsBuzy)
        {
            IsBuzy = true;
            BezierCurve clonedCurve = new BezierCurve(curve.positionCurve);
            lastUpdateTime = curve.lastMeshUpdateStartTime;

            MeshGenerator.curve = clonedCurve;
            MeshGenerator.RingPointCount = curve.ringPointCount;
            MeshGenerator.VertexSampleDistance = curve.GetVertexDensityDistance();
            MeshGenerator.tubeArcSampler = new FloatDistanceSampler(curve.arcOfTubeSampler);
            MeshGenerator.sizeSampler = new FloatDistanceSampler(curve.sizeSampler);
            MeshGenerator.rotationSampler = new FloatDistanceSampler(curve.rotationSampler);
            MeshGenerator.colorSampler = new ColorDistanceSampler(curve.colorSampler);
            MeshGenerator.doubleBezierSampler = new DoubleBezierSampler(curve.doubleBezierSampler);
            MeshGenerator.thicknessSampler = new FloatDistanceSampler(curve.thicknessSampler);
            MeshGenerator.clampAndStretchMeshToCurve = curve.clampAndStretchMeshToCurve;
            MeshGenerator.IsClosedLoop = curve.isClosedLoop;
            MeshGenerator.CurveType = curve.type;
            MeshGenerator.meshToTile = curve.meshToTile == null ? null : new ThreadMesh(curve.meshToTile);
            MeshGenerator.closeTilableMeshGap = curve.closeTilableMeshGap;
            MeshGenerator.meshPrimaryAxis = curve.meshPrimaryAxis;

            /*
            if (curve.displacementTextureColors!=null)
            {
                int len = curve.displacementTextureColors.Length;
                MeshGenerator.displacementColors = new Color32[len];
                Array.Copy(curve.displacementTextureColors, MeshGenerator.displacementColors, len);
            } 
            else
            {
                MeshGenerator.displacementColors = null;
            }
            */

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

    private delegate Vector3 PointCreator(PointOnCurve point, int pointNum, int totalPointCount, float size, float rotation, float offset,float pointDistance);
    private static bool GenerateMesh()
    {
        //Debug.Log("started thread");
        int numVerts;
        float curveLength = curve.GetLength();
        var sampled = curve.GetPointsWithSpacing(VertexSampleDistance);
        if (IsClosedLoop)
        {
            var lastPoint = new PointOnCurve(sampled[0]);
            lastPoint.distanceFromStartOfCurve = curveLength;
            lastPoint.time = 1.0f;
            lastPoint.segmentIndex = curve.NumSegments - 1;
            sampled.Add(lastPoint);
        }
        int numRings = sampled.Count - 1;
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
        float GetDistanceByArea(float area)
        {
            return sizeSampler.GetDistanceByAreaUnderInverseCurve(area, IsClosedLoop, curveLength, curve);
        }
        float GetSizeAtDistance(float distance)
        {
            return sizeSampler.GetValueAtDistance(distance, IsClosedLoop, curveLength, curve);
        }
        float GetTubeArcAtDistance(float distance)
        {
            return tubeArcSampler.GetValueAtDistance(distance, IsClosedLoop, curveLength, curve);
        }
        float GetThicknessAtDistance(float distance)
        {
            return thicknessSampler.GetValueAtDistance(distance, IsClosedLoop, curveLength, curve);
        }
        Color32 GetColorAtDistance(float distance)
        {
            return colorSampler.GetValueAtDistance(distance, IsClosedLoop, curveLength, curve);
        }
        sizeSampler.RecalculateOpenCurveOnlyPoints(curve);
        //var rand = new System.Random();
        void TrianglifyLayer(bool isExterior, int numPointsPerRing,int startIndex)
        {//generate tris
            int numVertsInALayer = (numRings+1) * numPointsPerRing;
            //int basePoint = isExterior ? 0 :numVerts/2;
            int basePoint = startIndex;
            for (int i = 0; i < numRings; i++)
            {
                int ringIndex = (i * numPointsPerRing)%numVertsInALayer;
                int nextRingIndex = (ringIndex + numPointsPerRing)%numVertsInALayer;
                for (int j = 0; j < numPointsPerRing; j++)
                {
                    if ((j + 1) >= numPointsPerRing)
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
        void CreateEdgeVertsTrisAndUvs(List<EdgePointInfo> edgePointInfos, bool flip=false)
        {
            int prevs1p1=-1;
            int prevs1p2=-1;
            int prevs2p1=-1;
            int prevs2p2=-1;

            int s1p1 = -1;
            int s1p2 = -1;
            int s2p1 = -1;
            int s2p2 = -1;

            for (int i=0;i<edgePointInfos.Count;i++)
            {
                var curr = edgePointInfos[i];
                vertices.Add(curr.side1Point1);
                vertices.Add(curr.side1Point2);
                vertices.Add(curr.side2Point1);
                vertices.Add(curr.side2Point2);

                var color = GetColorAtDistance(curr.distanceAlongCurve);
                var thickness = GetThicknessAtDistance(curr.distanceAlongCurve);

                colors.Add(color);
                colors.Add(color);
                colors.Add(color);
                colors.Add(color);

                var uvX = curr.distanceAlongCurve/thickness;

                uvs.Add(new Vector2(uvX,0));
                uvs.Add(new Vector2(uvX,1));
                uvs.Add(new Vector2(uvX,0));
                uvs.Add(new Vector2(uvX,1));

                s1p1 = vertices.Count - 4;
                s1p2 = vertices.Count - 3;
                s2p1 = vertices.Count - 2;
                s2p2 = vertices.Count - 1;
                if (i > 0)
                {
                    if (!flip)
                    {
                        DrawQuad(s1p1, s1p2, prevs1p1, prevs1p2);
                        DrawQuad(prevs2p1, prevs2p2, s2p1, s2p2);
                    }
                    else
                    {
                        DrawQuad(prevs1p1, prevs1p2,s1p1, s1p2);
                        DrawQuad(s2p1, s2p2,prevs2p1, prevs2p2);
                    }

                }
                prevs1p1 = s1p1;
                prevs1p2 = s1p2;
                prevs2p1 = s2p1;
                prevs2p2 = s2p2;
            }
        }
        List<EdgePointInfo> GetEdgePointInfo(int vertsPerRing)
        {
            List<EdgePointInfo> retr = new List<EdgePointInfo>();
            //foreach ring
            int numVertsPerLayer = vertsPerRing * sampled.Count;
            for (int i = 0; i < sampled.Count; i++)
            {
                var curr = sampled[i];
                int lowerIndex = 0 +vertsPerRing*i;
                int upperIndex = numVertsPerLayer + vertsPerRing * i;
                int s1p1 = lowerIndex;
                int s1p2 = upperIndex;
                int s2p1 = lowerIndex + vertsPerRing - 1;
                int s2p2 = upperIndex + vertsPerRing - 1;
                retr.Add(new EdgePointInfo() {
                    distanceAlongCurve = curr.distanceFromStartOfCurve,
                    side1Point1 = vertices[s1p1],
                    side1Point2 = vertices[s1p2],
                    side2Point1 = vertices[s2p1],
                    side2Point2 = vertices[s2p2]
                });
            }
            return retr;
        }
        void CreatePointsAlongCurve(PointCreator pointCreator,List<PointOnCurve> points, float offset, int pointsPerRing, bool offsetInterior)
        {
            float uvx=0;
            float previousLength = -1;
            for (int i = 0; i < points.Count; i++)
            {
                PointOnCurve currentPoint = points[i];
                float currOffset = offset * GetThicknessAtDistance(currentPoint.distanceFromStartOfCurve);
                var size = Mathf.Max(0, GetSizeAtDistance(currentPoint.distanceFromStartOfCurve) + (offsetInterior?currOffset:0));
                var rotation = rotationSampler.GetValueAtDistance(currentPoint.distanceFromStartOfCurve, IsClosedLoop, curveLength, curve);
                Color32 color = GetColorAtDistance(currentPoint.distanceFromStartOfCurve);
                float currentLength = 0;
                Vector3 previousPoint = Vector3.zero;
                for (int j = 0; j < pointsPerRing; j++)
                {
                    var position = pointCreator(currentPoint, j, pointsPerRing, size, rotation,currOffset,currentPoint.distanceFromStartOfCurve);
                    vertices.Add(position);
                    if (j > 0)
                        currentLength += Vector3.Distance(previousPoint,position);
                    previousPoint = position;
                }
                if (i > 0)
                {
                    float previousDistanceAlongCurve = points[i - 1].distanceFromStartOfCurve;
                    float currentDistanceAlongCurve = currentPoint.distanceFromStartOfCurve;
                    float averageLength = (previousLength + currentLength) / 2.0f;
                    uvx += (currentDistanceAlongCurve-previousDistanceAlongCurve)/ averageLength;
                }
                previousLength = currentLength;
                for (int point = 0; point < pointsPerRing; point++)
                {
                    uvs.Add(new Vector2(uvx,point/(float)(pointsPerRing-1)));
                    colors.Add(color);
                }
            }
        }
        Vector3 DoubleBezierPointCreator(PointOnCurve point, int currentIndex, int totalPointCount, float size, float rotation, float offset,float pointDistance)
        {
            float progress = currentIndex / (float)totalPointCount;
            var relativePos = doubleBezierSampler.SampleAt(point.distanceFromStartOfCurve, progress, curve, out Vector3 reference);
            //Lets say z is forward
            var cross = Vector3.Cross(point.tangent, point.reference).normalized;
            Vector3 TransformVector3(Vector3 vect)
            {
                return (Quaternion.LookRotation(point.tangent, point.reference) * vect);
            }
            var absolutePos = point.position + TransformVector3(relativePos);
            return absolutePos + TransformVector3(reference).normalized * offset;
        }
        Vector3 RectanglePointCreator(PointOnCurve point, int currentIndex, int totalPointCount, float size, float rotation, float offset,float pointDistance)
        {
            var center = point.position;
            var up = Quaternion.AngleAxis(rotation, point.tangent) * point.reference.normalized;
            var right = Vector3.Cross(up, point.tangent).normalized;
            var scaledUp = up*offset;
            var scaledRight = right * size;
            Vector3 lineStart = center + scaledUp + scaledRight;
            Vector3 lineEnd = center + scaledUp - scaledRight;
            return Vector3.Lerp(lineStart, lineEnd, currentIndex / (float)(totalPointCount - 1));
        }
        Vector3 TubePointCreator(PointOnCurve point, int currentIndex, int totalPointCount, float size, float rotation, float offset,float pointDistance)
        {
            float arc = GetTubeArcAtDistance(pointDistance);
            float theta = (arc * currentIndex / (totalPointCount - 1)) + (360.0f-arc)/ 2 + rotation;
            Vector3 rotatedVect = Quaternion.AngleAxis(theta, point.tangent) * point.reference;
            return point.GetRingPoint(theta, size);
        }
        Vector3 TubeFlatPlateCreator(PointOnCurve point, int currentIndex, int totalPointCount, float size, float rotation, float offset,float pointDistance)
        {
            Vector3 lineStart = TubePointCreator(point, 0, totalPointCount, size, rotation, offset,pointDistance);
            Vector3 lineEnd = TubePointCreator(point, totalPointCount-1, totalPointCount, size, rotation, offset,pointDistance);
            return Vector3.Lerp(lineStart, lineEnd, currentIndex / (float)(totalPointCount - 1));
        }
        void InitLists()
        {
            InitOrClear(ref vertices);
            InitOrClear(ref triangles);
            InitOrClear(ref uvs);
            InitOrClear(ref colors);
        }
        void CreateMeshInteriorExteriorEndPlates(int vertsPerRing,bool flip=false)
        {
            int interiorBase = numVerts / 2;
            //Then we gotta connect the ends as well
            int lastRingIndex = numRings * vertsPerRing;
            int firstRingIndex = 0;
            for (int j = 0; j < vertsPerRing; j++)
            {
                if ((j + 1) >= vertsPerRing)//will introduce a bug where curve never closes, even when angle is 360 TODO: revist
                    continue;
                if (!flip)
                {
                    DrawQuad(
                            firstRingIndex + ((j + 1) % vertsPerRing),
                            firstRingIndex + j,
                            firstRingIndex + ((j + 1) % vertsPerRing) + interiorBase,
                            firstRingIndex + j + interiorBase
                        );
                    DrawQuad(
                            lastRingIndex + j,
                            lastRingIndex + ((j + 1) % vertsPerRing),
                            lastRingIndex + j + interiorBase,
                            lastRingIndex + ((j + 1) % vertsPerRing) + interiorBase
                        );
                } 
                else
                {
                    DrawQuad(
                            firstRingIndex + ((j + 1) % vertsPerRing) + interiorBase,
                            firstRingIndex + j + interiorBase,
                            firstRingIndex + ((j + 1) % vertsPerRing),
                            firstRingIndex + j
                        );
                    DrawQuad(
                            lastRingIndex + j + interiorBase,
                            lastRingIndex + ((j + 1) % vertsPerRing) + interiorBase,
                            lastRingIndex + j,
                            lastRingIndex + ((j + 1) % vertsPerRing)
                        );
                }
            }
        }
        /*
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
        */
        switch (CurveType)
        {
            case CurveType.Cylinder:
                {
                    numVerts = RingPointCount* sampled.Count;
                    InitLists();
                    CreatePointsAlongCurve(TubePointCreator,sampled,.5f,RingPointCount,true);
                    CreatePointsAlongCurve(TubeFlatPlateCreator, sampled,.5f, 3, true);
                    //CreateRingPointsAlongCurve(sampled, ActualRingPointCount, true);
                    TrianglifyLayer(true, RingPointCount,0);
                    TrianglifyLayer(false, 3,numVerts);
                    /*
                    if (!IsClosedLoop)
                        CreateTubeEndPlates();
                        */
                    return true;
                }
            case CurveType.HollowTube:
                #region hollowtube
                {
                    numVerts = RingPointCount * sampled.Count * 2;
                    InitLists();
                    CreatePointsAlongCurve(TubePointCreator,sampled,.5f,RingPointCount,true);
                    CreatePointsAlongCurve(TubePointCreator,sampled,-.5f,RingPointCount,false);
                    TrianglifyLayer(true, RingPointCount,0);
                    TrianglifyLayer(false, RingPointCount,numVerts/2);
                    CreateEdgeVertsTrisAndUvs(GetEdgePointInfo(RingPointCount));
                    if (!IsClosedLoop)
                        CreateMeshInteriorExteriorEndPlates(RingPointCount);
                    return true;
                }
            #endregion
            case CurveType.Flat:
                #region flat
                {
                    int pointsPerFace = RingPointCount;
                    numVerts = 2*pointsPerFace * sampled.Count;
                    InitLists();
                    CreatePointsAlongCurve(RectanglePointCreator, sampled, .25f, pointsPerFace,false);
                    CreatePointsAlongCurve(RectanglePointCreator, sampled, -.25f, pointsPerFace,false);
                    TrianglifyLayer(true, pointsPerFace,0);
                    TrianglifyLayer(false, pointsPerFace,numVerts/2);
                    CreateEdgeVertsTrisAndUvs(GetEdgePointInfo(pointsPerFace));
                    if (!IsClosedLoop)
                        CreateMeshInteriorExteriorEndPlates(pointsPerFace);
                    return true;
                }
            #endregion
            case CurveType.DoubleBezier:
                {
                    List<Vector3> backSideBuffer = new List<Vector3>();
                    doubleBezierSampler.RecalculateOpenCurveOnlyPoints(curve);
                    int pointCount = RingPointCount;
                    numVerts = 2 * pointCount*sampled.Count;
                    InitLists();
                    CreatePointsAlongCurve(DoubleBezierPointCreator, sampled, -.25f, pointCount, true);
                    CreatePointsAlongCurve(DoubleBezierPointCreator, sampled, .25f, pointCount, true);
                    TrianglifyLayer(false, pointCount,0);
                    TrianglifyLayer(true, pointCount,numVerts/2);
                    CreateEdgeVertsTrisAndUvs(GetEdgePointInfo(pointCount),true);
                    if (!IsClosedLoop)
                        CreateMeshInteriorExteriorEndPlates(pointCount,true);
                    return true;
                }
            case CurveType.Mesh:
                {
                    InitLists();
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
                        return sizeSampler.GetValueAtDistance(dist, IsClosedLoop, curveLength, curve);
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
                            float distance;
                            float scaler = 120;
                            if (clampAndStretchMeshToCurve)
                                distance = (vert.x / meshLength) * curveLength;
                            else
                                distance = vert.x/scaler+c*(meshLength+closeTilableMeshGap)/scaler;//fast alternative
                                //distance = GetDistanceByArea((vert.x + c * (closeTilableMeshGap + meshLength)) / secondaryDimensionLength);
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
                            var rotation = rotationSampler.GetValueAtDistance(distance, IsClosedLoop, curveLength, curve);
                            var size = GetSize(distance);
                            var sizeScale = size / secondaryDimensionLength;
                            var reference = Quaternion.AngleAxis(rotation, point.tangent) * point.reference;
                            var cross = Vector3.Cross(reference, point.tangent);
                            vertices.Add(point.position + reference * vert.y * sizeScale + cross * vert.z * sizeScale);
                            colors.Add(GetColorAtDistance(distance));
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
