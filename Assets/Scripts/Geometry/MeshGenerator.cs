using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
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
        public static List<Vector2> uvs;
        public static List<Color32> colors;
        public static List<List<int>> submeshes;
        public static int submeshCount=0;

        public static int currentlyGeneratingCurve3D = -1;

        private static int _currentCurve3Did = 0;
        public static int GetCurve3DID()
        {
            _currentCurve3Did++;
            return _currentCurve3Did;
        }

        public static bool IsBuzy = false;

        public static DateTime lastUpdateTime;

        public static BezierCurve curve;

        //public static Color32[] displacementColors;

        public static bool useSubmeshes = true;

        public static ExtrudeSampler extrudeSampler;
        public static FloatDistanceSampler sizeSampler;
        public static FloatDistanceSampler rotationSampler;
        public static ColorDistanceSampler colorSampler;
        public static FloatDistanceSampler tubeArcSampler;
        public static FloatDistanceSampler thicknessSampler;

        public static TextureLayerSettings capTextureLayer;
        public static TextureLayerSettings flatTextureLayer;
        public static TextureLayerSettings alternateFlatTextureLayer;
        public static TextureLayerSettings tubeTextureLayer;
        public static TextureLayerSettings alternateTubeTextureLayer;
        public static TextureLayerSettings edgeTextureLayer;

        public static int RingPointCount = 2;
        public static int EdgePointCount = 20;
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
                currentlyGeneratingCurve3D = curve.GetMeshGenerationID();
                BezierCurve clonedCurve = new BezierCurve(curve.positionCurve, false);
                lastUpdateTime = curve.lastMeshUpdateStartTime;

                MeshGenerator.curve = clonedCurve;
                MeshGenerator.RingPointCount = curve.ringPointCount;
                MeshGenerator.VertexSampleDistance = curve.GetVertexDensityDistance();
                MeshGenerator.tubeArcSampler = new FloatDistanceSampler(curve.arcOfTubeSampler, false);
                MeshGenerator.sizeSampler = new FloatDistanceSampler(curve.sizeSampler, false);
                MeshGenerator.rotationSampler = new FloatDistanceSampler(curve.rotationSampler, false);
                MeshGenerator.colorSampler = new ColorDistanceSampler(curve.colorSampler, false);
                MeshGenerator.extrudeSampler = new ExtrudeSampler(curve.extrudeSampler, false);
                MeshGenerator.thicknessSampler = new FloatDistanceSampler(curve.thicknessSampler, false);
                MeshGenerator.clampAndStretchMeshToCurve = curve.clampAndStretchMeshToCurve;

                MeshGenerator.capTextureLayer = curve.capTextureLayer.settings;
                MeshGenerator.flatTextureLayer = curve.flatTextureLayer.settings;
                MeshGenerator.alternateFlatTextureLayer = curve.alternateFlatTextureLayer.settings;
                MeshGenerator.tubeTextureLayer = curve.tubeTextureLayer.settings;
                MeshGenerator.alternateTubeTextureLayer= curve.alternateTubeTextureLayer.settings;
                MeshGenerator.edgeTextureLayer = curve.edgeTextureLayer.settings;

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
        private static void InitOrClear<T>(ref List<T> list, int capacity = -1)
        {
            if (list == null)
            {
                if (capacity <= 0)
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
        private static void InitSubmeshes(int numSubmeshes)
        {
            submeshCount = numSubmeshes;
            if (submeshes == null)
                submeshes = new List<List<int>>();
            for (int i = submeshes.Count; i < numSubmeshes; i++)
                submeshes.Add(new List<int>());
            foreach (var i in submeshes)
                i.Clear();
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

        private delegate void UVCreator(int pointsPerRing,float uvx,PointOnCurve currentPoint,float size,TextureStretchDirection stretchDirection,float textureScale);
        private delegate Vector3 PointCreator(PointOnCurve point, int pointNum, int totalPointCount, float size, float rotation, float offset);
        private static bool GenerateMesh()
        {
            //Debug.Log("started thread");
            int numVerts;
            float curveLength = curve.GetLength();
            var sampled = curve.GetPointsWithSpacing(VertexSampleDistance);
            sizeSampler.RecalculateOpenCurveOnlyPoints(curve);
            if (IsClosedLoop)
            {
                var lastPoint = new PointOnCurve(sampled[0]);
                lastPoint.distanceFromStartOfCurve = curveLength;
                lastPoint.time = 1.0f;
                lastPoint.segmentIndex = curve.NumSegments - 1;
                sampled.Add(lastPoint);
            }
            int numRings = sampled.Count - 1;
            #region local functions
            void DrawQuad(int side1Point1, int side1Point2, int side2Point1, int side2Point2,int submeshIndex)
            {
                var triangles = submeshes[submeshIndex];
                //Tri1
                triangles.Add(side1Point1);
                triangles.Add(side2Point2);
                triangles.Add(side2Point1);
                //Tri2
                triangles.Add(side1Point1);
                triangles.Add(side1Point2);
                triangles.Add(side2Point2);
            }
            void DrawTri(int point1, int point2, int point3, int submeshIndex)
            {
                var triangles = submeshes[submeshIndex];
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
                return sizeSampler.GetValueAtDistance(distance, curve);
            }
            float GetTubeArcAtDistance(float distance)
            {
                return tubeArcSampler.GetValueAtDistance(distance, curve);
            }
            float GetThicknessAtDistance(float distance)
            {
                return thicknessSampler.GetValueAtDistance(distance, curve);
            }
            Color32 GetColorAtDistance(float distance)
            {
                return colorSampler.GetValueAtDistance(distance, curve);
            }
            void TrianglifyLayer(bool isExterior, int numPointsPerRing, int startIndex,int submeshIndex)
            {//generate tris
                int numVertsInALayer = (numRings + 1) * numPointsPerRing;
                int basePoint = startIndex;
                for (int i = 0; i < numRings; i++)
                {
                    int ringIndex = (i * numPointsPerRing) % numVertsInALayer;
                    int nextRingIndex = (ringIndex + numPointsPerRing) % numVertsInALayer;
                    for (int j = 0; j < numPointsPerRing; j++)
                    {
                        if ((j + 1) >= numPointsPerRing)
                            continue;

                        if (isExterior)
                            DrawQuad(
                                ringIndex + j + basePoint,
                                ringIndex + ((j + 1) % numPointsPerRing) + basePoint,
                                nextRingIndex + j + basePoint,
                                nextRingIndex + ((j + 1) % numPointsPerRing) + basePoint,
                                submeshIndex);
                        else //flipped
                            DrawQuad(
                                ringIndex + ((j + 1) % numPointsPerRing) + basePoint,
                                ringIndex + j + basePoint,
                                nextRingIndex + ((j + 1) % numPointsPerRing) + basePoint,
                                nextRingIndex + j + basePoint,
                                submeshIndex);
                    }
                }
            }
            void CreateEdgeVertsTrisAndUvs(List<EdgePointInfo> edgePointInfos, int submeshIndex, TextureLayerSettings textureLayer,bool flip = false)
            {
                int prevs1p1 = -1;
                int prevs1p2 = -1;
                int prevs2p1 = -1;
                int prevs2p2 = -1;
                int s1p1 = -1;
                int s1p2 = -1;
                int s2p1 = -1;
                int s2p2 = -1;

                UVCreator uvCreator = GetUVCreator(textureLayer.textureGenMode);

                for (int i = 0; i < edgePointInfos.Count; i++)
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

                    var uvX = curr.distanceAlongCurve / thickness;

                    uvs.Add(new Vector2(uvX, 0));
                    uvs.Add(new Vector2(uvX, 1));
                    uvs.Add(new Vector2(uvX, 0));
                    uvs.Add(new Vector2(uvX, 1));

                    s1p1 = vertices.Count - 4;
                    s1p2 = vertices.Count - 3;
                    s2p1 = vertices.Count - 2;
                    s2p2 = vertices.Count - 1;
                    if (i > 0)
                    {
                        if (!flip)
                        {
                            DrawQuad(s1p1, s1p2, prevs1p1, prevs1p2,submeshIndex);
                            DrawQuad(prevs2p1, prevs2p2, s2p1, s2p2,submeshIndex);
                        }
                        else
                        {
                            DrawQuad(prevs1p1, prevs1p2, s1p1, s1p2,submeshIndex);
                            DrawQuad(s2p1, s2p2, prevs2p1, prevs2p2,submeshIndex);
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
                    int lowerIndex = 0 + vertsPerRing * i;
                    int upperIndex = numVertsPerLayer + vertsPerRing * i;
                    int s1p1 = lowerIndex;
                    int s1p2 = upperIndex;
                    int s2p1 = lowerIndex + vertsPerRing - 1;
                    int s2p2 = upperIndex + vertsPerRing - 1;
                    retr.Add(new EdgePointInfo()
                    {
                        distanceAlongCurve = curr.distanceFromStartOfCurve,
                        side1Point1 = vertices[s1p1],
                        side1Point2 = vertices[s1p2],
                        side2Point1 = vertices[s2p1],
                        side2Point2 = vertices[s2p2]
                    });
                }
                return retr;
            }
            void CreateEndPlates(PointCreator pointCreator, float farUVx, int pointsPerRing, int submeshIndex, TextureGenerationMode texGenMode,bool flip = false)
            {
                void Partial_CreateEndPlates(bool isStartPlate)
                {
                    PointOnCurve point;
                    if (isStartPlate)
                        point = sampled[0];
                    else
                        point = sampled.Last();

                    int ring1Base = vertices.Count;
                    CreateRing(pointCreator, pointsPerRing, .5f, point,out _);

                    int ring2Base = vertices.Count;
                    CreateRing(pointCreator, pointsPerRing, -.5f, point,out _);

                    switch (texGenMode)
                    {
                        case TextureGenerationMode.TileLengthStretchWidth:
                        case TextureGenerationMode.StretchLengthStretchWidth:
                            float stretchFactor=1;
                            if (texGenMode == TextureGenerationMode.TileLengthStretchWidth)
                            {
                                float length = 0;
                                for (int i = ring1Base; i < ring1Base + pointsPerRing - 1; i++)
                                    length += Vector3.Distance(vertices[i], vertices[i + 1]);
                                float thickness = GetThicknessAtDistance(point.distanceFromStartOfCurve);
                                stretchFactor = length / thickness;
                            }
                            else if (texGenMode == TextureGenerationMode.StretchLengthStretchWidth)
                            {
                                stretchFactor = 1;
                            }
                            float mul = stretchFactor / (pointsPerRing - 1);
                            for (int i = 0; i < pointsPerRing; i++)
                                uvs.Add(new Vector2(i * mul, 0));
                            for (int i = 0; i < pointsPerRing; i++)
                                uvs.Add(new Vector2(i * mul, 1));
                            break;
                    }

                    bool side = isStartPlate;
                    if (flip)
                        side = !side;

                    for (int i = 0; i < pointsPerRing - 1; i++)
                    {
                        int side1 = ring1Base + i;
                        int side2 = ring2Base + i;
                        if (side)
                            DrawQuad(side1 + 1, side1, side2 + 1, side2,submeshIndex);
                        else
                            DrawQuad(side1, side1 + 1, side2, side2 + 1,submeshIndex);
                    }
                }
                Partial_CreateEndPlates(true);
                Partial_CreateEndPlates(false);
            }
            void CreateCylinderEndPlate(PointCreator pointCreator, bool isStartPlate, float farUVx, int pointsPerRing)
            {
                throw new NotImplementedException();
            }
            float CreateRing(PointCreator pointCreator, int pointsPerRing, float offset, PointOnCurve currentPoint,out float size)
            {
                GetColorSizeRotationThickness(currentPoint.distanceFromStartOfCurve, offset, out float outOffset, out size, out float rotation, out Color color);
                float currentLength = 0;
                Vector3 previousPoint = Vector3.zero;
                for (int j = 0; j < pointsPerRing; j++)
                {
                    var position = pointCreator(currentPoint, j, pointsPerRing, size, rotation, outOffset);
                    vertices.Add(position);
                    if (j > 0)
                        currentLength += Vector3.Distance(previousPoint, position);
                    previousPoint = position;
                    colors.Add(color);
                }
                return currentLength;
            }
            void GetColorSizeRotationThickness(float distance, float offset, out float outOffset, out float outSize, out float outRotation, out Color outColor)
            {
                outOffset = offset * GetThicknessAtDistance(distance);
                outSize = Mathf.Max(0, GetSizeAtDistance(distance));
                outRotation = rotationSampler.GetValueAtDistance(distance, curve);
                outColor = GetColorAtDistance(distance);
            }
            void TileUVCreator(int pointsPerRing, float uvx, PointOnCurve currentPoint, float size,TextureStretchDirection stretchDirection,float textureScale)
            {
                uvx = textureScale * uvx;
                for (int point = 0; point < pointsPerRing; point++)
                {
                    float uvy = textureScale*point / (float)(pointsPerRing - 1);
                    if (stretchDirection == TextureStretchDirection.x)
                        uvs.Add(new Vector2(uvx, uvy));
                    else if (stretchDirection == TextureStretchDirection.y)
                        uvs.Add(new Vector2(uvy, uvx));
                }
            }
            void StretchUVCreator(int pointsPerRing, float uvx, PointOnCurve currentPoint, float size,TextureStretchDirection stretchDirection,float textureScale)
            {
                for (int point = 0; point < pointsPerRing; point++)
                {
                    uvs.Add(new Vector2(currentPoint.distanceFromStartOfCurve / curveLength, point / (float)(pointsPerRing - 1)));
                }
            }
            void CylinderCrossSectionUVCreator(int pointsPerRing, float uvx, PointOnCurve currentPoint, float size,TextureStretchDirection stretchDirection,float textureScale)
            {
                int startIndex = vertices.Count - pointsPerRing;
                float diameter = 2 * size;
                var up = currentPoint.reference;
                var right = Vector3.Cross(up, currentPoint.tangent);
                up /= diameter;
                right /= diameter;
                for (int point = 0; point < pointsPerRing; point++)
                {
                    Vector3 pos = vertices[point + startIndex];
                    var relative = pos - currentPoint.position;
                    var x = Vector3.Dot(relative, right);
                    var y = Vector3.Dot(relative, up);
                    uvs.Add(new Vector2(x + .5f, y + .5f));
                }
            }
            List<float> tileLengthTileWidthUVCreatorBuffer = new List<float>();
            void TileLengthTileWidthUVCreator(int pointsPerRing, float uvx, PointOnCurve currentPoint, float size,TextureStretchDirection stretchDirection,float textureScale)
            {
                tileLengthTileWidthUVCreatorBuffer.Clear();
                //the texture should move away fromt the middle
                Vector3 prev = Vector3.zero;
                float dist = 0;
                int startIndex = vertices.Count - pointsPerRing;
                for (int point = 0; point < pointsPerRing; point++)
                {
                    Vector3 pos = vertices[point + startIndex];
                    if (point > 0)
                    {
                        dist += Vector3.Distance(pos, prev);
                    }
                    prev = pos;
                    tileLengthTileWidthUVCreatorBuffer.Add(dist);
                }
                for (int point = 0; point < pointsPerRing; point++)
                {
                    float x = textureScale * currentPoint.distanceFromStartOfCurve/size;
                    float y = textureScale*(tileLengthTileWidthUVCreatorBuffer[point] - dist / 2)/size;
                    if (stretchDirection == TextureStretchDirection.x)
                        uvs.Add(new Vector2(x,y));
                    else if (stretchDirection == TextureStretchDirection.y)
                        uvs.Add(new Vector2(y,x));
                }
            }
            UVCreator GetUVCreator(TextureGenerationMode texGenMode)
            {
                switch (texGenMode)
                {
                    case TextureGenerationMode.TileLengthStretchWidth:
                        return TileUVCreator;
                    case TextureGenerationMode.StretchLengthStretchWidth:
                        return StretchUVCreator;
                    case TextureGenerationMode.CrossSection:
                        switch (CurveType)
                        {
                            case CurveType.Cylinder:
                                return CylinderCrossSectionUVCreator;
                            default:
                                throw new System.ArgumentException($"'{texGenMode}' not supported for curve type '{CurveType}'");
                        }
                    case TextureGenerationMode.TileLengthTileWidth:
                        return TileLengthTileWidthUVCreator;
                    default:
                        throw new System.ArgumentException($"'{texGenMode}' Not Supported");
                }
            }
            void CreatePointsAlongCurve(PointCreator pointCreator, List<PointOnCurve> points, float offset, int pointsPerRing, out float farEndUV, TextureLayerSettings textureLayer)
            {
                UVCreator uvCreator = GetUVCreator(textureLayer.textureGenMode);
                TextureStretchDirection stretchDirection = textureLayer.stretchDirection;
                float textureScale = 1.0f/textureLayer.scale;
                float uvx = 0;
                float previousLength = -1;
                for (int i = 0; i < points.Count; i++)
                {
                    PointOnCurve currentPoint = points[i];
                    float currentLength = CreateRing(pointCreator, pointsPerRing, offset, currentPoint,out float size);
                    if (i > 0)
                    {
                        float previousDistanceAlongCurve = points[i - 1].distanceFromStartOfCurve;
                        float currentDistanceAlongCurve = currentPoint.distanceFromStartOfCurve;
                        float averageLength = (previousLength + currentLength) / 2.0f;
                        uvx += (currentDistanceAlongCurve - previousDistanceAlongCurve) / averageLength;
                    }
                    previousLength = currentLength;
                    uvCreator.Invoke(pointsPerRing, uvx, currentPoint,size,stretchDirection,textureScale);
                }
                farEndUV = uvx;
            }
            Vector3 ExtrudePointCreator(PointOnCurve point, int currentIndex, int totalPointCount, float size, float rotation, float offset)
            {
                float progress = currentIndex / (float)totalPointCount;
                var relativePos = extrudeSampler.SampleAt(point.distanceFromStartOfCurve, progress, curve, out Vector3 reference, out Vector3 tangent);//*size;
                var rotationMat = Quaternion.AngleAxis(rotation, point.tangent);
                //Lets say z is forward
                var cross = Vector3.Cross(point.tangent, point.reference).normalized;
                Vector3 TransformVector3(Vector3 vect)
                {
                    return (Quaternion.LookRotation(point.tangent, point.reference) * vect);
                }
                var absolutePos = point.position + rotationMat * TransformVector3(relativePos);
                Vector3 thicknessDirection = Vector3.Cross(reference, tangent);
                if (Vector3.Dot(TransformVector3(reference), point.tangent) < 0)
                    thicknessDirection *= -1;
                return absolutePos + (rotationMat * TransformVector3(thicknessDirection)).normalized * offset;
            }
            Vector3 RectanglePointCreator(PointOnCurve point, int currentIndex, int totalPointCount, float size, float rotation, float offset)
            {
                var center = point.position;
                var up = Quaternion.AngleAxis(rotation, point.tangent) * point.reference.normalized;
                var right = Vector3.Cross(up, point.tangent).normalized;
                var scaledUp = up * offset;
                var scaledRight = right * size;
                Vector3 lineStart = center + scaledUp + scaledRight;
                Vector3 lineEnd = center + scaledUp - scaledRight;
                return Vector3.Lerp(lineStart, lineEnd, currentIndex / (float)(totalPointCount - 1));
            }
            Vector3 TubePointCreator(PointOnCurve point, int currentIndex, int totalPointCount, float size, float rotation, float offset)
            {
                float arc = GetTubeArcAtDistance(point.distanceFromStartOfCurve);
                float theta = (arc * currentIndex / (totalPointCount - 1)) + (360.0f - arc) / 2 + rotation;
                Vector3 rotatedVect = Quaternion.AngleAxis(theta, point.tangent) * point.reference;
                return point.GetRingPoint(theta, (size + offset));
            }
            Vector3 TubeFlatPlateCreator(PointOnCurve point, int currentIndex, int totalPointCount, float size, float rotation, float offset)
            {
                Vector3 lineStart = TubePointCreator(point, 0, totalPointCount, size, rotation, offset);
                Vector3 lineEnd = TubePointCreator(point, totalPointCount - 1, totalPointCount, size, rotation, offset);
                return Vector3.Lerp(lineStart, lineEnd, currentIndex / (float)(totalPointCount - 1));
            }
            void InitLists()
            {
                InitOrClear(ref vertices);
                InitOrClear(ref uvs);
                InitOrClear(ref colors);
            }
            void CreateTubeEndPlates(float uvAtEnd,int submeshIndex)
            {

                int ActualRingPointCount = RingPointCount;
                //center point is average of ring
                int startRingBaseIndex = 0;
                int endRingBaseIndex = numRings * ActualRingPointCount;
                //add verts for each plate
                int newStartPlateBaseIndex = vertices.Count;
                int newEndPlateBaseIndex = newStartPlateBaseIndex + ActualRingPointCount + 1;//plus 1 vert for center vert
                void AddPlate(int baseOffset,float distance)
                {
                    var color = GetColorAtDistance(distance);
                    var point = curve.GetPointAtDistance(distance);
                    //float thickness = GetThicknessAtDistance(distance);
                    float size = GetSizeAtDistance(distance);
                    float diameter = 2 * size;
                    var up = point.reference;
                    var right = Vector3.Cross(up,point.tangent);
                    up /= diameter;
                    right /= diameter;
                    Vector2 GetUV(Vector3 pos)
                    {
                        var relative = pos - point.position;
                        var x = Vector3.Dot(relative, right);
                        var y = Vector3.Dot(relative, up);
                        return new Vector2(x+.5f,y+.5f);
                    }
                    Vector3 average = Vector3.zero;
                    for (int i = baseOffset; i < baseOffset + ActualRingPointCount; i++)
                    {
                        var vert = vertices[i];
                        average += vert;
                        vertices.Add(vert);
                        colors.Add(color);
                        uvs.Add(GetUV(vert));
                    }
                    average = average / ActualRingPointCount;
                    vertices.Add(average);
                    colors.Add(color);
                    uvs.Add(GetUV(average));
                }
                AddPlate(startRingBaseIndex,0);
                AddPlate(endRingBaseIndex,curveLength);
                void TrianglifyRingToCenter(int baseIndex, int centerIndex, bool invert)
                {
                    for (int i = 0; i < ActualRingPointCount; i++)
                    {
                        if (invert)
                            DrawTri(
                                baseIndex + i,
                                centerIndex,
                                baseIndex + ((i + 1) % ActualRingPointCount),
                                submeshIndex
                                );
                        else
                            DrawTri(
                                baseIndex + i,
                                baseIndex + ((i + 1) % ActualRingPointCount),
                                centerIndex,
                                submeshIndex
                                );
                    }
                }
                TrianglifyRingToCenter(newStartPlateBaseIndex, newStartPlateBaseIndex + ActualRingPointCount, true);
                TrianglifyRingToCenter(newEndPlateBaseIndex, newEndPlateBaseIndex + ActualRingPointCount, false);
            }
            #endregion
            switch (CurveType)
            {
                case CurveType.NoMesh:
                    InitLists();
                    return true;
                case CurveType.Cylinder:
                    {
                        numVerts = RingPointCount * sampled.Count;
                        InitLists();
                        InitSubmeshes(useSubmeshes ? (!IsClosedLoop?3:2) : 1);
                        CreatePointsAlongCurve(TubePointCreator, sampled, 0, RingPointCount, out _, tubeTextureLayer);
                        CreatePointsAlongCurve(TubeFlatPlateCreator, sampled, 0, 3, out float uvAtEnd,flatTextureLayer);
                        //CreateRingPointsAlongCurve(sampled, ActualRingPointCount, true);
                        //CreateEdgeVertsTrisAndUvs(GetEdgePointInfo(RingPointCount));
                        //make a thing which creates points at each end using createpointsalongcurve and then uv those
                        TrianglifyLayer(true, RingPointCount, 0,useSubmeshes?0:0);
                        TrianglifyLayer(false, 3, numVerts,useSubmeshes?1:0);
                        if (!IsClosedLoop)
                            CreateTubeEndPlates(uvAtEnd,useSubmeshes?2:0);
                        return true;
                    }
                case CurveType.HollowTube:
                    {
                        numVerts = RingPointCount * sampled.Count * 2;
                        InitLists();
                        InitSubmeshes(useSubmeshes ? (!IsClosedLoop?4:3) : 1);
                        CreatePointsAlongCurve(TubePointCreator, sampled, .5f, RingPointCount, out _,tubeTextureLayer);
                        CreatePointsAlongCurve(TubePointCreator, sampled, -.5f, RingPointCount, out float farUVX,alternateTubeTextureLayer);
                        TrianglifyLayer(true, RingPointCount, 0,useSubmeshes?0:0);
                        TrianglifyLayer(false, RingPointCount, numVerts / 2,useSubmeshes?1:0);
                        CreateEdgeVertsTrisAndUvs(GetEdgePointInfo(RingPointCount),useSubmeshes?2:0,edgeTextureLayer);
                        if (!IsClosedLoop)
                            CreateEndPlates(TubePointCreator, farUVX, RingPointCount,useSubmeshes?3:0,capTextureLayer.textureGenMode);
                        return true;
                    }
                case CurveType.Flat:
                    {
                        int pointsPerFace = RingPointCount;
                        numVerts = 2 * pointsPerFace * sampled.Count;
                        InitLists();
                        InitSubmeshes(useSubmeshes ? (!IsClosedLoop?4:3) : 1);
                        CreatePointsAlongCurve(RectanglePointCreator, sampled, .5f, pointsPerFace, out _,flatTextureLayer);
                        CreatePointsAlongCurve(RectanglePointCreator, sampled, -.5f, pointsPerFace, out float farUVX,alternateFlatTextureLayer);
                        TrianglifyLayer(true, pointsPerFace, 0,useSubmeshes?0:0);
                        TrianglifyLayer(false, pointsPerFace, numVerts / 2,useSubmeshes?1:0);
                        CreateEdgeVertsTrisAndUvs(GetEdgePointInfo(pointsPerFace),useSubmeshes?2:0,edgeTextureLayer);
                        if (!IsClosedLoop)
                            CreateEndPlates(RectanglePointCreator, farUVX, pointsPerFace,useSubmeshes?3:0,capTextureLayer.textureGenMode);
                        return true;
                    }
                case CurveType.Extrude:
                    {
                        List<Vector3> backSideBuffer = new List<Vector3>();
                        extrudeSampler.RecalculateOpenCurveOnlyPoints(curve);
                        int pointCount = RingPointCount;
                        numVerts = 2 * pointCount * sampled.Count;
                        InitSubmeshes(useSubmeshes ? (!IsClosedLoop?4:3) : 1);
                        InitLists();
                        CreatePointsAlongCurve(ExtrudePointCreator, sampled, .5f, pointCount, out _, flatTextureLayer);
                        CreatePointsAlongCurve(ExtrudePointCreator, sampled, -.5f, pointCount, out float farUVX,alternateFlatTextureLayer);
                        TrianglifyLayer(true, pointCount, numVerts / 2,useSubmeshes?0:0);
                        TrianglifyLayer(false, pointCount, 0,useSubmeshes?1:0);
                        CreateEdgeVertsTrisAndUvs(GetEdgePointInfo(pointCount), useSubmeshes?2:0,edgeTextureLayer,true);
                        if (!IsClosedLoop)
                            CreateEndPlates(ExtrudePointCreator, farUVX, pointCount, useSubmeshes?3:0,capTextureLayer.textureGenMode,true);
                        return true;
                    }
                case CurveType.Mesh:
                    {
                        InitLists();
                        InitSubmeshes(1);
                        if (meshToTile == null)
                            return true;
                        //we are gonna assume that the largest dimension of the bounding box is the correct direction, and that the mesh is axis aligned and it is perpendicular to the edge of the bounding box
                        var bounds = meshToTile.bounds;
                        //watch out for square meshes
                        float meshLength = -1;
                        float secondaryDimensionLength = -1;
                        {
                            Quaternion rotation = Quaternion.identity;
                            void UseXAsMainAxis()
                            {
                                meshLength = bounds.extents.x * 2;
                                secondaryDimensionLength = Mathf.Max(bounds.extents.y, bounds.extents.z);
                                rotation = Quaternion.FromToRotation(Vector3.right, Vector3.right);//does nothing
                            }
                            void UseYAsMainAxis()
                            {
                                meshLength = bounds.extents.y * 2;
                                secondaryDimensionLength = Mathf.Max(bounds.extents.x, bounds.extents.z);
                                rotation = Quaternion.FromToRotation(Vector3.up, Vector3.right);
                            }
                            void UseZAsMainAxis()
                            {
                                meshLength = bounds.extents.z * 2;
                                secondaryDimensionLength = Mathf.Max(bounds.extents.x, bounds.extents.y);
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
                        bool useUvs = meshToTile.uv.Length == meshToTile.verts.Length;
                        float GetSize(float dist)
                        {
                            return sizeSampler.GetValueAtDistance(dist, curve);
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
                                    distance = GetDistanceByArea((vert.x + c * (closeTilableMeshGap + meshLength)) / secondaryDimensionLength);//optimize me!
                                max = Mathf.Max(max, distance);
                                if (distance > curveLength)
                                {
                                    remappedVerts.Add(-1);
                                    skippedVerts++;
                                    continue;
                                }
                                else
                                {
                                    remappedVerts.Add(i - skippedVerts);
                                }
                                var point = curve.GetPointAtDistance(distance);
                                var rotation = rotationSampler.GetValueAtDistance(distance, curve);
                                var size = GetSize(distance);
                                var sizeScale = size / secondaryDimensionLength;
                                var reference = Quaternion.AngleAxis(rotation, point.tangent) * point.reference;
                                var cross = Vector3.Cross(reference, point.tangent);
                                vertices.Add(point.position + reference * vert.y * sizeScale + cross * vert.z * sizeScale);
                                colors.Add(GetColorAtDistance(distance));
                                if (useUvs)
                                    uvs.Add(meshToTile.uv[i]);
                            }
                            for (int i = 0; i < meshToTile.tris.Length; i += 3)
                            {
                                var tri1 = meshToTile.tris[i];
                                var tri2 = meshToTile.tris[i + 1];
                                var tri3 = meshToTile.tris[i + 2];
                                int remappedTri1 = remappedVerts[tri1];
                                int remappedTri2 = remappedVerts[tri2];
                                int remappedTri3 = remappedVerts[tri3];
                                if (remappedTri1 == -1 || remappedTri2 == -1 || remappedTri3 == -1)
                                    continue;
                                submeshes[0].Add(remappedTri1 + vertexBaseOffset);
                                submeshes[0].Add(remappedTri2 + vertexBaseOffset);
                                submeshes[0].Add(remappedTri3 + vertexBaseOffset);
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
                        for (int i = 0; i < submeshes[0].Count; i += 3)
                        {
                            var swap = submeshes[0][i];
                            submeshes[0][i] = submeshes[0][i + 2];
                            submeshes[0][i + 2] = swap;
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
        public static T Last<T>(this List<T> lst, int indexFromLast = 0)
        {
            return lst[lst.Count - 1 - indexFromLast];
        }
    }
}
