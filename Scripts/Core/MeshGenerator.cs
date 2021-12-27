using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public static class MeshGenerator
    {
        public static bool didMeshGenerationSucceed;

        private struct EdgePointInfo
        {
            public float distanceAlongCurve;
            public Vector3 side1Point1;
            public Vector3 side1Point2;

            public Vector3 side2Point1;
            public Vector3 side2Point2;

            public Vector3 tangentDirection;
        }
        private static Vector3 NormalTangent(Vector3 forwardVector, Vector3 previous)
        {
            return Vector3.ProjectOnPlane(previous, forwardVector).normalized;
        }

        public static MeshGeneratorOutput GenerateMesh(MeshGeneratorData data)
        {
            MeshGeneratorOutput output = data.output;
            ExtrudeSampler extrudeSampler = data.extrudeSampler;
            FloatSampler sizeSampler = data.sizeSampler;
            FloatSampler rotationSampler = data.rotationSampler;
            ColorSampler colorSampler = data.colorSampler;
            FloatSampler tubeArcSampler = data.tubeArcSampler;
            FloatSampler thicknessSampler = data.thicknessSampler;
            BezierCurve curve = data.curve;
            TextureLayerSettings mainTextureLayer = data.mainTextureLayer;
            TextureLayerSettings backTextureLayer = data.backTextureLayer;
            TextureLayerSettings edgeTextureLayer = data.edgeTextureLayer;
            TextureLayerSettings endTextureLayer = data.endTextureLayer;
            int RingPointCount = data.RingPointCount;
            int FlatPointCount = data.FlatPointCount;
            int EdgePointCount = data.EdgePointCount;
            float VertexSampleDistance = data.VertexSampleDistance;
            bool clampAndStretchMeshToCurve = data.clampAndStretchMeshToCurve;
            bool IsClosedLoop = data.IsClosedLoop;
            MeshGenerationMode CurveType = data.CurveType;
            ThreadMesh meshToTile = data.meshToTile;
            float closeTilableMeshGap = data.closeTilableMeshGap;
            MeshPrimaryAxis meshPrimaryAxis = data.meshPrimaryAxis;
            //public int currentlyGeneratingForCurveId;

            extrudeSampler?.CacheDistances(curve);
            sizeSampler.CacheDistances(curve);
            colorSampler.CacheDistances(curve);
            tubeArcSampler.CacheDistances(curve);
            thicknessSampler.CacheDistances(curve);
            rotationSampler.CacheDistances(curve);

            List<int> triangles = output.triangles;
            List<MeshGeneratorVertexItem> vertexItems = output.data;
            List<SurfaceInfo> distances = output.distances;
            List<UnityEngine.Rendering.SubMeshDescriptor> submeshInfo = output.submeshInfo;

            int currentSubmeshBase = 0;
            void EndSubmesh()
            {
                submeshInfo.Add(new UnityEngine.Rendering.SubMeshDescriptor(currentSubmeshBase, triangles.Count-currentSubmeshBase));
                currentSubmeshBase = triangles.Count;
            }

            float curveLength = curve.GetLength();
            bool shouldTubeGenerateEdges = false;
            if (CurveType == MeshGenerationMode.HollowTube || CurveType == MeshGenerationMode.Cylinder)
            {
                if (!tubeArcSampler.UseKeyframes)
                {
                    if (tubeArcSampler.constValue != 360)
                        shouldTubeGenerateEdges = true;
                }
                else
                {
                    foreach (var i in tubeArcSampler.points)
                    {
                        if (i.value != 360)
                        {
                            shouldTubeGenerateEdges = true;
                            break;
                        }
                    }
                }
            }
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
            #region local functions
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
                return sizeSampler.GetValueAtDistance(distance, curve, true);
            }
            float GetTubeArcAtDistance(float distance)
            {
                return tubeArcSampler.GetValueAtDistance(distance, curve, true);
            }
            float GetThicknessAtDistance(float distance)
            {
                return thicknessSampler.GetValueAtDistance(distance, curve, true);
            }
            Color32 GetColorAtDistance(float distance)
            {
                return colorSampler.GetValueAtDistance(distance, curve, true);
            }
            float GetRotationAtDistance(float distance)
            {
                return rotationSampler.GetValueAtDistance(distance, curve, true);
            }
            void TrianglifyLayer(bool isExterior, int numPointsPerRing, int startIndex)
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
            void CreateEdgeVertsTrisAndUvs(List<EdgePointInfo> edgePointInfos, TextureLayerSettings textureLayer, bool flip)
            {
                List<float> distsFromStart = new List<float>();
                List<float> thicknessses = new List<float>();

                {
                    int prevs1p1 = -1;
                    int prevs1p2 = -1;
                    int s1p1 = -1;
                    int s1p2 = -1;
                    int uvIndex = vertexItems.Count;
                    for (int i = 0; i < edgePointInfos.Count; i++)
                    {
                        var curr = edgePointInfos[i];

                        var normal = (flip?1:-1)*Vector3.Cross(curr.side1Point2-curr.side1Point1,curr.tangentDirection).normalized;
                        var color = GetColorAtDistance(curr.distanceAlongCurve);
                        var thickness = GetThicknessAtDistance(curr.distanceAlongCurve);

                        distances.Add(new SurfaceInfo(curr.distanceAlongCurve,-1));
                        distances.Add(new SurfaceInfo(curr.distanceAlongCurve,-1));

                        MeshGeneratorVertexItem vertItem1 = new MeshGeneratorVertexItem();
                        MeshGeneratorVertexItem vertItem2 = new MeshGeneratorVertexItem();
                        vertItem1.position = curr.side1Point1;
                        vertItem2.position = curr.side1Point2;
                        vertItem1.normal = normal;
                        vertItem2.normal = normal;
                        vertItem1.color= color;
                        vertItem2.color= color;
                        vertexItems.Add(vertItem1);
                        vertexItems.Add(vertItem2);

                        var uvX = curr.distanceAlongCurve / thickness;

                        thicknessses.Add(thickness);
                        distsFromStart.Add(curr.distanceAlongCurve);

                        s1p1 = vertexItems.Count- 2;
                        s1p2 = vertexItems.Count- 1;
                        if (i > 0)
                        {
                            if (!flip)
                            {
                                DrawQuad(s1p1, s1p2, prevs1p1, prevs1p2);
                            }
                            else
                            {
                                DrawQuad(prevs1p1, prevs1p2, s1p1, s1p2);
                            }

                        }
                        prevs1p1 = s1p1;
                        prevs1p2 = s1p2;
                    }
                    CreateUVS(uvIndex,textureLayer, distsFromStart, thicknessses, 2);
                }

                {
                    int prevs2p1 = -1;
                    int prevs2p2 = -1;
                    int s2p1 = -1;
                    int s2p2 = -1;
                    int uvIndex = vertexItems.Count;
                    for (int i = 0; i < edgePointInfos.Count; i++)
                    {
                        var curr = edgePointInfos[i];
                        var normal = (flip?-1:1)*Vector3.Cross(curr.side2Point2-curr.side2Point1,curr.tangentDirection).normalized;
                        var color = GetColorAtDistance(curr.distanceAlongCurve);
                        var thickness = GetThicknessAtDistance(curr.distanceAlongCurve);
                        distances.Add(new SurfaceInfo(curr.distanceAlongCurve, -1));
                        distances.Add(new SurfaceInfo(curr.distanceAlongCurve, -1));

                        MeshGeneratorVertexItem vertItem1 = new MeshGeneratorVertexItem();
                        MeshGeneratorVertexItem vertItem2 = new MeshGeneratorVertexItem();
                        vertItem1.position = curr.side2Point1;
                        vertItem2.position = curr.side2Point2;
                        vertItem1.normal = normal;
                        vertItem2.normal = normal;
                        vertItem1.color = color;
                        vertItem2.color = color;
                        vertexItems.Add(vertItem1);
                        vertexItems.Add(vertItem2);

                        var uvX = curr.distanceAlongCurve / thickness;

                        s2p1 = vertexItems.Count - 2;
                        s2p2 = vertexItems.Count - 1;
                        if (i > 0)
                        {
                            if (!flip)
                            {
                                DrawQuad(prevs2p1, prevs2p2, s2p1, s2p2);
                            }
                            else
                            {
                                DrawQuad(s2p1, s2p2, prevs2p1, prevs2p2);
                            }

                        }
                        prevs2p1 = s2p1;
                        prevs2p2 = s2p2;
                    }
                    CreateUVS(uvIndex,textureLayer, distsFromStart, thicknessses, 2);
                }
            }
            List<EdgePointInfo> GetEdgePointInfo(int vertsPerRing)
            {
                List<EdgePointInfo> retr = new List<EdgePointInfo>();
                //foreach ring
                int numVertsPerLayer = vertsPerRing * sampled.Count;
                for (int i = 0; i < sampled.Count; i++)
                {
                    var point = sampled[i];
                    int lowerIndex = 0 + vertsPerRing * i;
                    int upperIndex = numVertsPerLayer + vertsPerRing * i;
                    int s1p1 = lowerIndex;
                    int s1p2 = upperIndex;
                    int s2p1 = lowerIndex + vertsPerRing - 1;
                    int s2p2 = upperIndex + vertsPerRing - 1;
                    retr.Add(new EdgePointInfo()
                    {
                        distanceAlongCurve = point.distanceFromStartOfCurve,
                        side1Point1 = vertexItems[s1p1].position,
                        side1Point2 = vertexItems[s1p2].position,
                        side2Point1 = vertexItems[s2p1].position,
                        side2Point2 = vertexItems[s2p2].position,
                        tangentDirection = point.tangent,
                    });
                }
                return retr;
            }
            void CreateUVS(int startIndex,TextureLayerSettings settings, List<float> distsFromStart, List<float> thicknesses, int pointsPerBand, Vector3? up = null, Vector3? right = null, Vector3? center = null)
            {
                int numBands = distsFromStart.Count;
                float surfaceLength = distsFromStart.Last();
                float tileUVX = 0;
                for (int bandIndex = 0; bandIndex < numBands; bandIndex++)
                {
                    var thickness = thicknesses[bandIndex];
                    var distFromStart = distsFromStart[bandIndex];
                    int bandOffset = startIndex + bandIndex * pointsPerBand;
                    void SetUV(int index, Vector2 uv)
                    {
                        var curr = vertexItems[bandOffset + index];
                        Vector3 avgX = Vector3.zero;
                        if (bandIndex > 0)
                        {
                            var prev = vertexItems[startIndex + (bandIndex - 1) * pointsPerBand+index];
                            Vector3 offset = curr.position - prev.position;
                            avgX += offset;
                        }
                        if (bandIndex < numBands-1)
                        {
                            var next = vertexItems[startIndex + (bandIndex + 1) * pointsPerBand+index];
                            Vector3 offset = next.position - curr.position;
                            avgX += offset;
                        }
                        Vector3 avgY = Vector3.zero;
                        if (index > 0)
                        {
                            var prev = vertexItems[bandOffset + index - 1];
                            Vector3 offset = curr.position - prev.position;
                            avgY += offset;
                        }
                        if (index < pointsPerBand - 1)
                        {
                            var next = vertexItems[bandOffset + index + 1];
                            Vector3 offset = next.position - curr.position;
                            avgY += offset;
                        }
                        Vector3 tangent;
                        float w;
                        if (settings.textureDirection == TextureDirection.x)
                        {
                            tangent = avgX.normalized;
                            w = Vector3.Dot(Vector3.Cross(curr.normal, tangent), avgY) >= 0 ? 1 : -1;
                        }
                        else
                        {
                            tangent = avgY.normalized;
                            w = Vector3.Dot(Vector3.Cross(curr.normal, tangent), avgX) >= 0 ? 1 : -1;
                        }
                        curr.uv = uv;
                        curr.tangent = new Vector4(tangent.x, tangent.y, tangent.z, w);
                        vertexItems[bandOffset+index] = curr;
                    }
                    switch (settings.textureGenMode)
                    {
                        case TextureGenerationMode.Stretch:
                            if (settings.textureDirection == TextureDirection.x)
                            {
                                for (int i = 0; i < pointsPerBand; i++)
                                {
                                    SetUV(i, new Vector2(distFromStart / surfaceLength, i / (float)(pointsPerBand - 1)));
                                }
                            }
                            else
                            {
                                for (int i = 0; i < pointsPerBand; i++)
                                {
                                    SetUV(i,new Vector2(i / (float)(pointsPerBand - 1), distFromStart / surfaceLength));
                                }
                            }
                            break;
                        case TextureGenerationMode.Tile:
                            if (bandIndex > 0)
                            {
                                float prevLength = distsFromStart[bandIndex - 1];
                                float currLength = distsFromStart[bandIndex];
                                float xDelta = currLength - prevLength;
                                float prevY = thicknesses[bandIndex - 1];
                                float currY = thicknesses[bandIndex];
                                float avgY = (prevY + currY) / 2;
                                tileUVX += xDelta / avgY;
                            }
                            float scaledUvx = settings.scale * tileUVX;
                            for (int i = 0; i < pointsPerBand; i++)
                            {
                                float uvy = settings.scale * i / (float)(pointsPerBand - 1);
                                if (settings.textureDirection == TextureDirection.x)
                                    SetUV(i,new Vector2(scaledUvx, uvy));
                                else if (settings.textureDirection == TextureDirection.y)
                                    SetUV(i,new Vector2(uvy, scaledUvx));
                            }
                            break;
                        case TextureGenerationMode.Flat:
                            if (right.HasValue && center.HasValue && up.HasValue)//special case for end plates
                            {
                                Vector3 rightNormalized = right.Value.normalized;
                                Vector3 upNormalized = up.Value.normalized;
                                for (int i = 0; i < pointsPerBand; i++)
                                {
                                    var curr = vertexItems[bandOffset + i];
                                    curr.uv = GetFlatUV(curr.position - center.Value, settings, right.Value, up.Value,curr.normal,rightNormalized,upNormalized,out Vector4 tangent);
                                    curr.tangent = tangent;
                                    vertexItems[bandOffset + i] = curr;
                                }
                            }
                            else
                            {
                                if (settings.textureDirection == TextureDirection.x)
                                {
                                    for (int i = 0; i < pointsPerBand; i++)
                                    {
                                        SetUV(i,new Vector2(settings.scale * distFromStart, settings.scale * thickness * Mathf.Lerp(-.5f, .5f, i / (float)pointsPerBand)));
                                    }
                                }
                                else
                                {
                                    for (int i = 0; i < pointsPerBand; i++)
                                    {
                                        SetUV(i,new Vector2(settings.scale * thickness * Mathf.Lerp(-.5f, .5f, i / (float)pointsPerBand), settings.scale * distFromStart));
                                    }
                                }
                            }
                            break;
                        default:
                            throw new NotImplementedException($"TextureGenerationMode '{settings.textureGenMode}' is not yet supported");
                    }
                }
            }
            void CreateEndPlate(bool isStartPlate, float distanceFromStart, PointCreator pointCreator, int pointsPerRing, TextureLayerSettings settings, float offset1, float offset2, bool flip = false)
            {
                PointOnCurve point;
                Vector3 normal;
                if (isStartPlate)
                {
                    point = sampled[0];
                    normal = -point.tangent;
                }
                else
                {
                    point = sampled.Last();
                    normal = point.tangent;
                }

                int baseUVIndex = vertexItems.Count;
                int ring1Base = vertexItems.Count;
                CreateRing(pointCreator, pointsPerRing, offset1, point, out _);

                int ring2Base = vertexItems.Count;
                CreateRing(pointCreator, pointsPerRing, offset2, point, out _);

                bool side = isStartPlate;
                if (flip)
                    side = !side;

                ///Reorders verts and colors so they'll work with the existing uv generation code
                List<Vector3> pos = new List<Vector3>();
                List<Color> color = new List<Color>();
                for (int i = 0; i < pointsPerRing; i++)
                {
                    pos.Add(vertexItems[ring1Base + i].position);
                    pos.Add(vertexItems[ring2Base + i].position);
                    color.Add(vertexItems[ring1Base + i].color);
                    color.Add(vertexItems[ring2Base + i].color);
                }
                for (int i = 0; i < pointsPerRing * 2; i++)
                {
                    MeshGeneratorVertexItem vertItem = new MeshGeneratorVertexItem();
                    vertItem.position = pos[i];
                    vertItem.color = color[i];
                    vertItem.normal = normal;
                    vertexItems[ring1Base + i] = vertItem;
                }
                //done reordering


                Vector3 previousCenter = Vector3.zero;
                float dist = 0;
                List<float> distancesFromStart = new List<float>();
                List<float> thicknesses = new List<float>();
                float thickness = Vector3.Distance(vertexItems[ring1Base].position, vertexItems[ring1Base + 1].position);
                for (int i = 0; i < pointsPerRing; i++)
                {
                    var centerPoint = (vertexItems[ring1Base + 2 * i].position + vertexItems[ring1Base + 1 + 2 * i].position) / 2;
                    if (i > 0)
                    {
                        dist += Vector3.Distance(previousCenter, centerPoint);
                    }
                    previousCenter = centerPoint;
                    distancesFromStart.Add(dist);
                    thicknesses.Add(thickness);
                }
                Vector3 up = point.reference;
                Vector3 right = Vector3.Cross(up, point.tangent);
                float diameter = GetSizeAtDistance(point.distanceFromStartOfCurve) * 2;
                up /= diameter;
                right /= diameter;
                CreateUVS(baseUVIndex,settings, distancesFromStart, thicknesses, 2, up, right, point.position);

                int end = ring1Base+2*(pointsPerRing-1);
                for (int i = ring1Base; i < end; i += 2)
                {
                    if (side)
                        DrawQuad(i, i + 1, i + 2, i + 3);
                    else
                        DrawQuad(i + 2, i + 3, i, i + 1);
                }
            }
            float CreateRing(PointCreator pointCreator, int pointsPerRing, float offset, PointOnCurve currentPoint, out float size)
            {
                GetColorSizeRotationThickness(currentPoint.distanceFromStartOfCurve, offset, out float outOffset, out size, out float rotation, out Color color);
                float currentLength = 0;
                Vector3 previousPoint = Vector3.zero;
                float arc = GetTubeArcAtDistance(currentPoint.distanceFromStartOfCurve);
                for (int j = 0; j < pointsPerRing; j++)
                {
                    var position = pointCreator(currentPoint, j, pointsPerRing, size, rotation, outOffset,arc,extrudeSampler,curve,true,out Vector3 normal,out float crosswise);
                    MeshGeneratorVertexItem item = new MeshGeneratorVertexItem();
                    item.position = position;
                    item.normal = normal;
                    item.color = color;
                    vertexItems.Add(item);
                    distances.Add(new SurfaceInfo(currentPoint.distanceFromStartOfCurve,crosswise));
                    if (j > 0)
                        currentLength += Vector3.Distance(previousPoint, position);
                    previousPoint = position;
                }
                return currentLength;
            }
            void GetColorSizeRotationThickness(float distance, float offset, out float outOffset, out float outSize, out float outRotation, out Color outColor)
            {
                outOffset = offset * GetThicknessAtDistance(distance);
                outSize = Mathf.Max(0, GetSizeAtDistance(distance));
                outRotation = GetRotationAtDistance(distance);
                outColor = GetColorAtDistance(distance);
            }
            void CreatePointsAlongCurve(PointCreator pointCreator, List<PointOnCurve> points, float offset, int pointsPerRing, TextureLayerSettings textureLayer)
            {
                TextureDirection stretchDirection = textureLayer.textureDirection;
                float textureScale = 1.0f / textureLayer.scale;
                List<float> distsFromStart = new List<float>();
                List<float> thickness = new List<float>();
                int baseUVIndex = vertexItems.Count;
                for (int i = 0; i < points.Count; i++)
                {
                    PointOnCurve currentPoint = points[i];
                    float currentLength = CreateRing(pointCreator, pointsPerRing, offset, currentPoint, out float size);
                    distsFromStart.Add(currentPoint.distanceFromStartOfCurve);
                    thickness.Add(currentLength);
                }
                CreateUVS(baseUVIndex,textureLayer, distsFromStart, thickness, pointsPerRing);
            }
            /*
                var up = point.reference;
                var right = Vector3.Cross(up, point.tangent);
            */
            Vector2 GetFlatUV(Vector3 relative, TextureLayerSettings settings, Vector3 right, Vector3 up, Vector3 normal,Vector3 rightNormalized, Vector3 upNormalized,out Vector4 tangent)
            {
                var x = Vector3.Dot(relative, right) * settings.scale;
                var y = Vector3.Dot(relative, up) * settings.scale;
                if (settings.textureDirection == TextureDirection.x)
                {
                    float w = Vector3.Dot(Vector3.Cross(right, normal), up) >= 0 ? -1 : 1;
                    tangent = new Vector4(rightNormalized.x,rightNormalized.y,rightNormalized.z,w);
                    return new Vector2(x + .5f, y + .5f);
                }
                else
                {
                    float w = Vector3.Dot(Vector3.Cross(up, normal), right) >= 0 ? -1 : 1;
                    tangent = new Vector4(upNormalized.x,upNormalized.y,upNormalized.z,w);
                    return new Vector2(y + .5f, x + .5f);
                }
            }
            void CreateTubeEndPlates()
            {

                int ActualRingPointCount = RingPointCount;
                //center point is average of ring
                int startRingBaseIndex = 0;
                int endRingBaseIndex = numRings * ActualRingPointCount;
                //add verts for each plate
                int newStartPlateBaseIndex = vertexItems.Count;
                int newEndPlateBaseIndex = newStartPlateBaseIndex + ActualRingPointCount + 1;//plus 1 vert for center vert
                void AddPlate(int baseOffset, bool start)
                {
                    float distance = start ? 0 : curveLength;
                    var color = GetColorAtDistance(distance);
                    var point = curve.GetPointAtDistance(distance);
                    //float thickness = GetThicknessAtDistance(distance);
                    float size = GetSizeAtDistance(distance);
                    float diameter = 2 * size;
                    var up = point.reference;
                    var right = Vector3.Cross(up, point.tangent);
                    up /= diameter;
                    right /= diameter;
                    Vector3 average = Vector3.zero;
                    Vector3 normal = start ?-point.tangent:point.tangent;
                    for (int i = baseOffset; i < baseOffset + ActualRingPointCount; i++)
                    {
                        var vert = vertexItems[i].position;
                        average += vert;
                        MeshGeneratorVertexItem item = new MeshGeneratorVertexItem();
                        item.position = vert;
                        item.normal = normal;
                        item.color = color;
                        item.uv = GetFlatUV(vert - point.position, endTextureLayer, right, up,normal,right.normalized,up.normalized,out Vector4 tangent);
                        item.tangent = tangent;
                        vertexItems.Add(item);
                        distances.Add(new SurfaceInfo(distance,-1));
                    }
                    {
                        MeshGeneratorVertexItem item = new MeshGeneratorVertexItem();
                        average = average / ActualRingPointCount;
                        item.position = average;
                        item.normal = normal;
                        item.color = color;
                        item.uv = GetFlatUV(average - point.position, endTextureLayer, right, up,normal,right.normalized,up.normalized,out Vector4 tangent);
                        item.tangent = tangent;
                        vertexItems.Add(item);
                        distances.Add(new SurfaceInfo(distance, -1));
                    }
                }
                AddPlate(startRingBaseIndex, true);
                AddPlate(endRingBaseIndex, false);
                void TrianglifyRingToCenter(int baseIndex, int centerIndex, bool invert)
                {
                    for (int i = 0; i < ActualRingPointCount; i++)
                    {
                        if (invert)
                            DrawTri(
                                baseIndex + i,
                                centerIndex,
                                baseIndex + ((i + 1) % ActualRingPointCount));
                        else
                            DrawTri(
                                baseIndex + i,
                                baseIndex + ((i + 1) % ActualRingPointCount),
                                centerIndex);
                    }
                }
                TrianglifyRingToCenter(newStartPlateBaseIndex, newStartPlateBaseIndex + ActualRingPointCount, true);
                TrianglifyRingToCenter(newEndPlateBaseIndex, newEndPlateBaseIndex + ActualRingPointCount, false);
            }
            #endregion
            switch (CurveType)
            {
                case MeshGenerationMode.NoMesh:
                    return output;
                case MeshGenerationMode.Cylinder:
                    {
                        int numMainLayerVerts = RingPointCount * sampled.Count;
                        CreatePointsAlongCurve(MeshGeneratorPointCreators.TubePointCreator, sampled, 0, RingPointCount, mainTextureLayer);
                        if (shouldTubeGenerateEdges)
                            CreatePointsAlongCurve(MeshGeneratorPointCreators.TubeFlatPlateCreator, sampled, 0, FlatPointCount, backTextureLayer);
                        TrianglifyLayer(true, RingPointCount, 0);
                        EndSubmesh();
                        if (shouldTubeGenerateEdges)
                        {
                            TrianglifyLayer(false, FlatPointCount, numMainLayerVerts);
                            EndSubmesh();
                        }
                        if (!IsClosedLoop)
                        {
                            CreateTubeEndPlates();
                            EndSubmesh();
                        }
                        return output;
                    }
                case MeshGenerationMode.HollowTube:
                    { 
                        int numMainLayerVerts = RingPointCount * sampled.Count * 2;

                        CreatePointsAlongCurve(MeshGeneratorPointCreators.TubePointCreator, sampled, MeshGeneratorPointCreators.frontOffset, RingPointCount, mainTextureLayer);
                        CreatePointsAlongCurve(MeshGeneratorPointCreators.TubePointCreator, sampled, MeshGeneratorPointCreators.backOffset, RingPointCount, backTextureLayer);

                        TrianglifyLayer(true, RingPointCount, 0);
                        EndSubmesh();
                        TrianglifyLayer(false, RingPointCount, numMainLayerVerts / 2);
                        EndSubmesh();

                        if (shouldTubeGenerateEdges)
                        {
                            CreateEdgeVertsTrisAndUvs(GetEdgePointInfo(RingPointCount), edgeTextureLayer, false);
                            EndSubmesh();
                        }
                        if (!IsClosedLoop)
                        {
                            CreateEndPlate(true, 0, MeshGeneratorPointCreators.TubePointCreator, RingPointCount, endTextureLayer, MeshGeneratorPointCreators.frontOffset, MeshGeneratorPointCreators.backOffset);
                            CreateEndPlate(false, curve.GetLength(), MeshGeneratorPointCreators.TubePointCreator, RingPointCount, endTextureLayer, MeshGeneratorPointCreators.frontOffset, MeshGeneratorPointCreators.backOffset);
                            EndSubmesh();
                        }

                        return output;
                    }
                case MeshGenerationMode.Flat:
                    {
                        int pointsPerFace = FlatPointCount;
                        int numMainLayerVerts = 2 * pointsPerFace * sampled.Count;
                        CreatePointsAlongCurve(MeshGeneratorPointCreators.RectanglePointCreator, sampled,MeshGeneratorPointCreators.frontOffset, pointsPerFace, mainTextureLayer);
                        CreatePointsAlongCurve(MeshGeneratorPointCreators.RectanglePointCreator, sampled, MeshGeneratorPointCreators.backOffset, pointsPerFace, backTextureLayer);
                        TrianglifyLayer(true, pointsPerFace, 0);
                        EndSubmesh();
                        TrianglifyLayer(false, pointsPerFace, numMainLayerVerts / 2);
                        EndSubmesh();
                        CreateEdgeVertsTrisAndUvs(GetEdgePointInfo(pointsPerFace), edgeTextureLayer, false);
                        EndSubmesh();
                        if (!IsClosedLoop)
                        {
                            CreateEndPlate(true, 0, MeshGeneratorPointCreators.RectanglePointCreator, RingPointCount, endTextureLayer, MeshGeneratorPointCreators.frontOffset, MeshGeneratorPointCreators.backOffset);
                            CreateEndPlate(false, curve.GetLength(), MeshGeneratorPointCreators.RectanglePointCreator, RingPointCount, endTextureLayer, MeshGeneratorPointCreators.frontOffset, MeshGeneratorPointCreators.backOffset);
                            EndSubmesh();
                        }
                        return output;
                    }
                case MeshGenerationMode.Extrude:
                    {
                        List<Vector3> backSideBuffer = new List<Vector3>();
                        extrudeSampler.RecalculateOpenCurveOnlyPoints(curve);
                        int pointCount = RingPointCount;
                        int numMainLayerVerts = 2 * pointCount * sampled.Count;
                        CreatePointsAlongCurve(MeshGeneratorPointCreators.ExtrudePointCreator, sampled, MeshGeneratorPointCreators.frontOffset, pointCount, mainTextureLayer);
                        CreatePointsAlongCurve(MeshGeneratorPointCreators.ExtrudePointCreator, sampled, MeshGeneratorPointCreators.backOffset, pointCount, backTextureLayer);
                        TrianglifyLayer(true, pointCount, numMainLayerVerts / 2);
                        EndSubmesh();
                        TrianglifyLayer(false, pointCount, 0);
                        EndSubmesh();
                        CreateEdgeVertsTrisAndUvs(GetEdgePointInfo(pointCount), edgeTextureLayer, true);
                        EndSubmesh();
                        if (!IsClosedLoop)
                        {
                            CreateEndPlate(true, 0, MeshGeneratorPointCreators.ExtrudePointCreator, pointCount, endTextureLayer, MeshGeneratorPointCreators.frontOffset, MeshGeneratorPointCreators.backOffset, true);
                            CreateEndPlate(false, curve.GetLength(), MeshGeneratorPointCreators.ExtrudePointCreator, pointCount, endTextureLayer, MeshGeneratorPointCreators.frontOffset, MeshGeneratorPointCreators.backOffset, true);
                            EndSubmesh();
                        }
                        return output;
                    }
                case MeshGenerationMode.Mesh:
                    {
                        if (meshToTile == null)
                            return output;
                        //we are gonna assume that the largest dimension of the bounding box is the correct direction, and that the mesh is axis aligned and it is perpendicular to the edge of the bounding box
                        var bounds = meshToTile.bounds;
                        //watch out for square meshes
                        float meshLength = -1;
                        bool useUvs = meshToTile.uv!=null && meshToTile.uv.Length == meshToTile.verts.Length;
                        bool useTangents = meshToTile.tangents!=null && meshToTile.tangents.Length == meshToTile.verts.Length;
                        Quaternion initialRotation = Quaternion.identity;
                        float secondaryDimensionLength = -1;
                        {
                            void UseXAsMainAxis()
                            {
                                meshLength = bounds.extents.x * 2;
                                secondaryDimensionLength = Mathf.Max(bounds.extents.y, bounds.extents.z);
                                initialRotation = Quaternion.FromToRotation(Vector3.right, Vector3.right);//does nothing
                            }
                            void UseYAsMainAxis()
                            {
                                meshLength = bounds.extents.y * 2;
                                secondaryDimensionLength = Mathf.Max(bounds.extents.x, bounds.extents.z);
                                initialRotation = Quaternion.FromToRotation(Vector3.up, Vector3.right);
                            }
                            void UseZAsMainAxis()
                            {
                                meshLength = bounds.extents.z * 2;
                                secondaryDimensionLength = Mathf.Max(bounds.extents.x, bounds.extents.y);
                                initialRotation = Quaternion.FromToRotation(Vector3.forward, Vector3.right);
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
                                return (initialRotation * (point - bounds.center)) + new Vector3(meshLength / 2, 0, 0);
                            }
                            for (int i = 0; i < meshToTile.verts.Length; i++)
                            {
                                meshToTile.verts[i] = TransformPoint(meshToTile.verts[i]);
                                meshToTile.verts[i].x = Mathf.Max(0, meshToTile.verts[i].x);//clamp above zero, sometimes floats mess with this
                                if (useTangents)
                                {
                                    var transformedTangent = initialRotation*meshToTile.tangents[i];
                                    meshToTile.tangents[i] = new Vector4(transformedTangent.x,transformedTangent.y,transformedTangent.z,meshToTile.tangents[i].w);
                                }
                                meshToTile.normals[i] = initialRotation*meshToTile.normals[i];
                            }
                            //now x is always along the mesh and normalized around the center
                        }
                        int vertCount = meshToTile.verts.Length;
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
                                var rotation = GetRotationAtDistance(distance);
                                var size = GetSizeAtDistance(distance);
                                var sizeScale = size / secondaryDimensionLength;
                                var localRotation = Quaternion.AngleAxis(rotation, point.tangent);
                                var reference = localRotation * point.reference;
                                var cross = Vector3.Cross(reference, point.tangent);
                                MeshGeneratorVertexItem item = new MeshGeneratorVertexItem();
                                item.position = point.position + reference * vert.y * sizeScale + cross * vert.z * sizeScale;
                                item.color = GetColorAtDistance(distance);
                                if (useUvs)
                                    item.uv = meshToTile.uv[i];
                                else
                                    item.uv = new Vector2();
                                var normal = meshToTile.normals[i];
                                var transformedNormal = (normal.x * point.tangent + reference * normal.y + cross * normal.z).normalized;
                                item.normal = transformedNormal;
                                if (useTangents)
                                {
                                    var tangent = meshToTile.tangents[i];
                                    var transformedTangent = (tangent.x*point.tangent+reference * tangent.y + cross * tangent.z).normalized;
                                    var ogBitangent = Vector3.Cross(tangent, normal);
                                    var transformedOGBitangent = (ogBitangent.x*point.tangent+reference * ogBitangent.y + cross * ogBitangent.z);
                                    var newBitangent = Vector3.Cross(transformedTangent,transformedNormal);
                                    item.tangent = new Vector4(transformedTangent.x, transformedTangent.y, transformedTangent.z, tangent.w*(Vector3.Dot(transformedOGBitangent, newBitangent) > 0 ? 1:-1));
                                }
                                else
                                {
                                    item.tangent = new Vector4(1, 0, 0, 1);
                                }
                                vertexItems.Add(item);
                                distances.Add(new SurfaceInfo(point.distanceFromStartOfCurve,-1));
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
                                triangles.Add(remappedTri1 + vertexBaseOffset);
                                triangles.Add(remappedTri2 + vertexBaseOffset);
                                triangles.Add(remappedTri3 + vertexBaseOffset);
                            }
                            vertexBaseOffset += vertCount - skippedVerts;
                            c++;
                            f = max;
                        }
                        ///end temp
                        for (int i = 0; i < triangles.Count; i += 3)
                        {
                            var swap = triangles[i];
                            triangles[i] = triangles[i + 2];
                            triangles[i + 2] = swap;
                        }
                        EndSubmesh();
                        return output;
                    }
                default:
                    throw new NotImplementedException();
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
