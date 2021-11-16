using System;
using System.Collections.Generic;
using System.Threading;
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
        }
        private static Vector3 NormalTangent(Vector3 forwardVector, Vector3 previous)
        {
            return Vector3.ProjectOnPlane(previous, forwardVector).normalized;
        }

        private delegate Vector3 PointCreator(PointOnCurve point, int pointNum, int totalPointCount, float size, float rotation, float offset, float arc);
        public static MeshGeneratorOutput GenerateMesh(MeshGeneratorData data)
        {
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


            MeshGeneratorOutput output = new MeshGeneratorOutput();
            List<List<int>> submeshes = null;
            List<Vector3> vertices = output.vertices;
            List<Vector2> uvs = output.uvs;
            List<Color32> colors = output.colors;
            List<Vector3> normals = output.normals;

            List<(int, int)> smoothNormals = new List<(int, int)>();

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
            void DrawQuad(int side1Point1, int side1Point2, int side2Point1, int side2Point2, int submeshIndex)
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
            void CreateNormals()
            {
                for (int i = 0; i < vertices.Count; i++)
                    normals.Add(Vector3.zero);
                for (int submeshIndex = 0; submeshIndex < submeshes.Count; submeshIndex++)
                {
                    int end = submeshes[submeshIndex].Count;
                    for (int i = 0; i < end; i += 3)
                    {
                        int v1 = submeshes[submeshIndex][i];
                        int v2 = submeshes[submeshIndex][i + 1];
                        int v3 = submeshes[submeshIndex][i + 2];
                        Vector3 p1 = vertices[v1];
                        Vector3 p2 = vertices[v2];
                        Vector3 p3 = vertices[v3];
                        Vector3 normal = Vector3.Cross(p2 - p1, p3 - p1).normalized;
                        normals[v1]+= normal;
                        normals[v2]+= normal;
                        normals[v3]+= normal;
                    }
                }
                foreach (var i in smoothNormals)
                {
                    Vector3 normal1 = normals[i.Item1];
                    Vector3 normal2 = normals[i.Item2];
                    Vector3 sum = normal1 + normal2;
                    normals[i.Item1] = sum;
                    normals[i.Item2] = sum;
                }
                for (int i=0;i<normals.Count;i++)
                {
                    normals[i] = normals[i].normalized;
                }
            }
            void TrianglifyLayer(bool isExterior, int numPointsPerRing, int startIndex, int submeshIndex)
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
            void CreateEdgeVertsTrisAndUvs(List<EdgePointInfo> edgePointInfos, int submeshIndex, TextureLayerSettings textureLayer, bool flip)
            {
                List<float> distsFromStart = new List<float>();
                List<float> thicknessses = new List<float>();

                {
                    int prevs1p1 = -1;
                    int prevs1p2 = -1;
                    int s1p1 = -1;
                    int s1p2 = -1;
                    for (int i = 0; i < edgePointInfos.Count; i++)
                    {
                        var curr = edgePointInfos[i];

                        vertices.Add(curr.side1Point1);
                        vertices.Add(curr.side1Point2);

                        var color = GetColorAtDistance(curr.distanceAlongCurve);
                        var thickness = GetThicknessAtDistance(curr.distanceAlongCurve);

                        colors.Add(color);
                        colors.Add(color);

                        var uvX = curr.distanceAlongCurve / thickness;

                        thicknessses.Add(thickness);
                        distsFromStart.Add(curr.distanceAlongCurve);

                        s1p1 = vertices.Count - 2;
                        s1p2 = vertices.Count - 1;
                        if (i > 0)
                        {
                            if (!flip)
                            {
                                DrawQuad(s1p1, s1p2, prevs1p1, prevs1p2, submeshIndex);
                            }
                            else
                            {
                                DrawQuad(prevs1p1, prevs1p2, s1p1, s1p2, submeshIndex);
                            }

                        }
                        prevs1p1 = s1p1;
                        prevs1p2 = s1p2;
                    }
                    CreateUVS(textureLayer, distsFromStart, thicknessses, 2);
                }

                {
                    int prevs2p1 = -1;
                    int prevs2p2 = -1;
                    int s2p1 = -1;
                    int s2p2 = -1;
                    for (int i = 0; i < edgePointInfos.Count; i++)
                    {
                        var curr = edgePointInfos[i];

                        vertices.Add(curr.side2Point1);
                        vertices.Add(curr.side2Point2);

                        var color = GetColorAtDistance(curr.distanceAlongCurve);
                        var thickness = GetThicknessAtDistance(curr.distanceAlongCurve);

                        colors.Add(color);
                        colors.Add(color);

                        var uvX = curr.distanceAlongCurve / thickness;

                        s2p1 = vertices.Count - 2;
                        s2p2 = vertices.Count - 1;
                        if (i > 0)
                        {
                            if (!flip)
                            {
                                DrawQuad(prevs2p1, prevs2p2, s2p1, s2p2, submeshIndex);
                            }
                            else
                            {
                                DrawQuad(s2p1, s2p2, prevs2p1, prevs2p2, submeshIndex);
                            }

                        }
                        prevs2p1 = s2p1;
                        prevs2p2 = s2p2;
                    }
                    CreateUVS(textureLayer, distsFromStart, thicknessses, 2);
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
                        side1Point1 = vertices[s1p1],
                        side1Point2 = vertices[s1p2],
                        side2Point1 = vertices[s2p1],
                        side2Point2 = vertices[s2p2],
                    });
                }
                return retr;
            }
            void CreateUVS(TextureLayerSettings settings, List<float> distsFromStart, List<float> thicknesses, int pointsPerBand, Vector3? up = null, Vector3? right = null, Vector3? center = null)
            {
                int numBands = distsFromStart.Count;
                float surfaceLength = distsFromStart.Last();
                float tileUVX = 0;
                for (int bandIndex = 0; bandIndex < numBands; bandIndex++)
                {
                    var thickness = thicknesses[bandIndex];
                    var distFromStart = distsFromStart[bandIndex];
                    switch (settings.textureGenMode)
                    {
                        case TextureGenerationMode.Stretch://doesn't yet support stretch direction, but should
                            if (settings.textureDirection == TextureDirection.x)
                            {
                                for (int i = 0; i < pointsPerBand; i++)
                                    uvs.Add(new Vector2(distFromStart / surfaceLength, i / (float)(pointsPerBand - 1)));
                            }
                            else
                            {
                                for (int i = 0; i < pointsPerBand; i++)
                                    uvs.Add(new Vector2(i / (float)(pointsPerBand - 1), distFromStart / surfaceLength));
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
                                    uvs.Add(new Vector2(scaledUvx, uvy));
                                else if (settings.textureDirection == TextureDirection.y)
                                    uvs.Add(new Vector2(uvy, scaledUvx));
                            }
                            break;
                        case TextureGenerationMode.Flat:
                            if (right.HasValue && center.HasValue && up.HasValue)
                            {
                                for (int i = 0; i < pointsPerBand; i++)
                                {
                                    uvs.Add(GetFlatUV(vertices[uvs.Count] - center.Value, settings, right.Value, up.Value));
                                }
                            }
                            else
                            {
                                if (settings.textureDirection == TextureDirection.x)
                                {
                                    for (int i = 0; i < pointsPerBand; i++)
                                    {
                                        uvs.Add(new Vector2(settings.scale * distFromStart, settings.scale * thickness * Mathf.Lerp(-.5f, .5f, i / (float)pointsPerBand)));
                                    }
                                }
                                else
                                {
                                    for (int i = 0; i < pointsPerBand; i++)
                                    {
                                        uvs.Add(new Vector2(settings.scale * thickness * Mathf.Lerp(-.5f, .5f, i / (float)pointsPerBand), settings.scale * distFromStart));
                                    }
                                }
                            }
                            break;
                        default:
                            throw new NotImplementedException($"TextureGenerationMode '{settings.textureGenMode}' is not yet supported");
                    }
                }
            }
            void CreateEndPlate(bool isStartPlate, float distanceFromStart, PointCreator pointCreator, int pointsPerRing, int submeshIndex, TextureLayerSettings settings, float offset1, float offset2, bool flip = false)
            {
                PointOnCurve point;
                if (isStartPlate)
                    point = sampled[0];
                else
                    point = sampled.Last();

                int ring1Base = vertices.Count;
                CreateRing(pointCreator, pointsPerRing, offset1, point, out _);

                int ring2Base = vertices.Count;
                CreateRing(pointCreator, pointsPerRing, offset2, point, out _);

                bool side = isStartPlate;
                if (flip)
                    side = !side;

                ///Reorders verts and colors so they'll work with the existing uv generation code
                List<Vector3> pos = new List<Vector3>();
                List<Color> color = new List<Color>();
                for (int i = 0; i < pointsPerRing; i++)
                {
                    pos.Add(vertices[ring1Base + i]);
                    pos.Add(vertices[ring2Base + i]);
                    color.Add(colors[ring1Base + i]);
                    color.Add(colors[ring2Base + i]);
                }
                for (int i = 0; i < pointsPerRing * 2; i++)
                {
                    vertices[ring1Base + i] = pos[i];
                    colors[ring1Base + i] = color[i];
                }
                //done reordering


                Vector3 previousCenter = Vector3.zero;
                float dist = 0;
                List<float> distancesFromStart = new List<float>();
                List<float> thicknesses = new List<float>();
                float thickness = Vector3.Distance(vertices[ring1Base], vertices[ring1Base + 1]);
                for (int i = 0; i < pointsPerRing; i++)
                {
                    var centerPoint = (vertices[ring1Base + 2 * i] + vertices[ring1Base + 1 + 2 * i]) / 2;
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
                CreateUVS(settings, distancesFromStart, thicknesses, 2, up, right, point.position);

                int end = ring1Base+2*(pointsPerRing-1);
                for (int i = ring1Base; i < end; i += 2)
                {
                    if (side)
                        DrawQuad(i, i + 1, i + 2, i + 3, submeshIndex);
                    else
                        DrawQuad(i + 2, i + 3, i, i + 1, submeshIndex);
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
                    var position = pointCreator(currentPoint, j, pointsPerRing, size, rotation, outOffset,arc);
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
                outRotation = GetRotationAtDistance(distance);
                outColor = GetColorAtDistance(distance);
            }
            void CreatePointsAlongCurve(PointCreator pointCreator, List<PointOnCurve> points, float offset, int pointsPerRing, TextureLayerSettings textureLayer)
            {
                TextureDirection stretchDirection = textureLayer.textureDirection;
                float textureScale = 1.0f / textureLayer.scale;
                List<float> distsFromStart = new List<float>();
                List<float> thickness = new List<float>();
                for (int i = 0; i < points.Count; i++)
                {
                    PointOnCurve currentPoint = points[i];
                    float currentLength = CreateRing(pointCreator, pointsPerRing, offset, currentPoint, out float size);
                    distsFromStart.Add(currentPoint.distanceFromStartOfCurve);
                    thickness.Add(currentLength);
                }
                CreateUVS(textureLayer, distsFromStart, thickness, pointsPerRing);
            }
            Vector3 ExtrudePointCreator(PointOnCurve point, int currentIndex, int totalPointCount, float size, float rotation, float offset,float arc)
            {
                totalPointCount -= 1;
                float progress = currentIndex / (float)totalPointCount;
                var relativePos = extrudeSampler.SampleAt(point.distanceFromStartOfCurve, progress, curve, out Vector3 reference, out Vector3 tangent,true);//*size;
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
            Vector3 RectanglePointCreator(PointOnCurve point, int currentIndex, int totalPointCount, float size, float rotation, float offset,float arc)
            {
                var center = point.position;
                var up = Quaternion.AngleAxis(rotation, point.tangent) * point.reference;
                var right = Vector3.Cross(up, point.tangent).normalized;
                var scaledUp = up * offset;
                var scaledRight = right * size;
                Vector3 lineStart = center + scaledUp + scaledRight;
                Vector3 lineEnd = center + scaledUp - scaledRight;
                return Vector3.Lerp(lineStart, lineEnd, currentIndex / (float)(totalPointCount - 1));
            }
            Vector3 TubePointCreator(PointOnCurve point, int currentIndex, int totalPointCount, float size, float rotation, float offset,float arc)
            {
                float theta = (arc * currentIndex / (totalPointCount - 1)) + (360.0f - arc) / 2 + rotation;
                Vector3 rotatedVect = Quaternion.AngleAxis(theta, point.tangent) * point.reference;
                var pos = point.GetRingPoint(theta, (size + offset));
                return pos;
            }
            Vector3 TubeFlatPlateCreator(PointOnCurve point, int currentIndex, int totalPointCount, float size, float rotation, float offset,float arc)
            {
                Vector3 lineStart = TubePointCreator(point, 0, totalPointCount, size, rotation, offset,arc);
                Vector3 lineEnd = TubePointCreator(point, totalPointCount - 1, totalPointCount, size, rotation, offset,arc);
                float lerp = currentIndex / (float)(totalPointCount - 1);
                return Vector3.Lerp(lineStart, lineEnd, lerp);
            }
            /*
                var up = point.reference;
                var right = Vector3.Cross(up, point.tangent);
            */
            Vector2 GetFlatUV(Vector3 relative, TextureLayerSettings settings, Vector3 right, Vector3 up)
            {
                var x = Vector3.Dot(relative, right) * settings.scale;
                var y = Vector3.Dot(relative, up) * settings.scale;
                if (settings.textureDirection == TextureDirection.x)
                    return new Vector2(x + .5f, y + .5f);
                else
                    return new Vector2(y + .5f, x + .5f);
            }
            void CreateTubeEndPlates(int submeshIndex)
            {

                int ActualRingPointCount = RingPointCount;
                //center point is average of ring
                int startRingBaseIndex = 0;
                int endRingBaseIndex = numRings * ActualRingPointCount;
                //add verts for each plate
                int newStartPlateBaseIndex = vertices.Count;
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
                        var vert = vertices[i];
                        average += vert;
                        vertices.Add(vert);
                        colors.Add(color);
                        uvs.Add(GetFlatUV(vert - point.position, endTextureLayer, right, up));
                    }
                    average = average / ActualRingPointCount;
                    vertices.Add(average);
                    colors.Add(color);
                    uvs.Add(GetFlatUV(average - point.position, endTextureLayer, right, up));
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
                case MeshGenerationMode.NoMesh:
                    return output;
                case MeshGenerationMode.Cylinder:
                    {
                        int numMainLayerVerts = RingPointCount * sampled.Count;
                        output.InitSubmeshes(!IsClosedLoop ? 3 : 2,out submeshes);
                        CreatePointsAlongCurve(TubePointCreator, sampled, 0, RingPointCount, mainTextureLayer);
                        if (!shouldTubeGenerateEdges)
                            for (int i = 0; i < vertices.Count/RingPointCount; i++)
                                smoothNormals.Add((i*RingPointCount,(i+1)*RingPointCount-1));
                        if (shouldTubeGenerateEdges)
                            CreatePointsAlongCurve(TubeFlatPlateCreator, sampled, 0, FlatPointCount, backTextureLayer);
                        TrianglifyLayer(true, RingPointCount, 0, 0);
                        if (shouldTubeGenerateEdges)
                            TrianglifyLayer(false, FlatPointCount, numMainLayerVerts, 1);
                        if (!IsClosedLoop)
                        {
                            int submeshIndex = 2;
                            CreateTubeEndPlates(submeshIndex);
                        }
                        CreateNormals();
                        return output;
                    }
                case MeshGenerationMode.HollowTube:
                    {
                        int numMainLayerVerts = RingPointCount * sampled.Count * 2;
                        output.InitSubmeshes(!IsClosedLoop ? 4 : 3,out submeshes);

                        CreatePointsAlongCurve(TubePointCreator, sampled, 0, RingPointCount, mainTextureLayer);
                        CreatePointsAlongCurve(TubePointCreator, sampled, -1, RingPointCount, backTextureLayer);

                        if (!shouldTubeGenerateEdges)
                            for (int i = 0; i < vertices.Count/RingPointCount; i++)
                                smoothNormals.Add((i*RingPointCount,(i+1)*RingPointCount-1));

                        TrianglifyLayer(true, RingPointCount, 0, 0);
                        TrianglifyLayer(false, RingPointCount, numMainLayerVerts / 2, 1);

                        if (shouldTubeGenerateEdges)
                            CreateEdgeVertsTrisAndUvs(GetEdgePointInfo(RingPointCount), 2, edgeTextureLayer, false);
                        if (!IsClosedLoop)
                        {
                            int submeshIndex = 3;
                            CreateEndPlate(true, 0, TubePointCreator, RingPointCount, submeshIndex, endTextureLayer, 0, -1);
                            CreateEndPlate(false, curve.GetLength(), TubePointCreator, RingPointCount, submeshIndex, endTextureLayer, 0, -1);
                        }

                        CreateNormals();

                        return output;
                    }
                case MeshGenerationMode.Flat:
                    {
                        int pointsPerFace = FlatPointCount;
                        int numMainLayerVerts = 2 * pointsPerFace * sampled.Count;
                        output.InitSubmeshes(!IsClosedLoop ? 4 : 3,out submeshes);
                        CreatePointsAlongCurve(RectanglePointCreator, sampled, .5f, pointsPerFace, mainTextureLayer);
                        CreatePointsAlongCurve(RectanglePointCreator, sampled, -.5f, pointsPerFace, backTextureLayer);
                        TrianglifyLayer(true, pointsPerFace, 0, 0);
                        TrianglifyLayer(false, pointsPerFace, numMainLayerVerts / 2, 1);
                        CreateEdgeVertsTrisAndUvs(GetEdgePointInfo(pointsPerFace), 2, edgeTextureLayer, false);
                        if (!IsClosedLoop)
                        {
                            int submeshIndex = 3;
                            CreateEndPlate(true, 0, RectanglePointCreator, RingPointCount, submeshIndex, endTextureLayer, .5f, -.5f);
                            CreateEndPlate(false, curve.GetLength(), RectanglePointCreator, RingPointCount, submeshIndex, endTextureLayer, .5f, -.5f);
                        }
                        CreateNormals();
                        return output;
                    }
                case MeshGenerationMode.Extrude:
                    {
                        List<Vector3> backSideBuffer = new List<Vector3>();
                        extrudeSampler.RecalculateOpenCurveOnlyPoints(curve);
                        int pointCount = RingPointCount;
                        int numMainLayerVerts = 2 * pointCount * sampled.Count;
                        output.InitSubmeshes(!IsClosedLoop ? 4 : 3,out submeshes);
                        CreatePointsAlongCurve(ExtrudePointCreator, sampled, .5f, pointCount, mainTextureLayer);
                        CreatePointsAlongCurve(ExtrudePointCreator, sampled, -.5f, pointCount, backTextureLayer);
                        TrianglifyLayer(true, pointCount, numMainLayerVerts / 2, 1);
                        TrianglifyLayer(false, pointCount, 0, 0);
                        CreateEdgeVertsTrisAndUvs(GetEdgePointInfo(pointCount),  2, edgeTextureLayer, true);
                        if (!IsClosedLoop)
                        {
                            int submeshIndex = 3;
                            CreateEndPlate(true, 0, ExtrudePointCreator, pointCount, submeshIndex, endTextureLayer, .5f, -.5f, true);
                            CreateEndPlate(false, curve.GetLength(), ExtrudePointCreator, pointCount, submeshIndex, endTextureLayer, .5f, -.5f, true);
                        }
                        CreateNormals();
                        return output;
                    }
                case MeshGenerationMode.Mesh:
                    {
                        output.autoRecalculateNormals = true;
                        output.InitSubmeshes(1,out submeshes);
                        if (meshToTile == null)
                            return output;
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
                        ///end temp
                        for (int i = 0; i < submeshes[0].Count; i += 3)
                        {
                            var swap = submeshes[0][i];
                            submeshes[0][i] = submeshes[0][i + 2];
                            submeshes[0][i + 2] = swap;
                        }
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
