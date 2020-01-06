using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class MyGUI
{
    private static readonly int _BeizerHint = "MyGUI.Beizer".GetHashCode();

    private const int _pointHitboxSize = 10;

    #region gui tools
    private static Color DesaturateColor(Color color, float amount)
    {
        return Color.Lerp(color, Color.white, amount);
    }
    static void DrawPoint(Rect position, Color color, Texture2D tex)
    {
        Color oldColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(position,tex);
        GUI.color = oldColor;
    }
    private static Rect GetRectCenteredAtPosition(Vector2 position, int halfWidth, int halfHeight)
    {
        return new Rect(position.x - halfWidth, position.y - halfHeight, 2 * halfWidth, 2 * halfHeight);
    }
    private static bool WorldToGUISpace(Vector3 worldPos, out Vector2 guiPosition, out float screenDepth)
    {
        var sceneCam = UnityEditor.SceneView.lastActiveSceneView.camera;
        Vector3 screen_pos = sceneCam.WorldToScreenPoint(worldPos);
        screenDepth = screen_pos.z;
        if (screen_pos.z < 0)
        {
            guiPosition = Vector2.zero;
            return false;
        }
        guiPosition = ScreenSpaceToGuiSpace(screen_pos);
        return true;
    }
    private static Vector3 GUIToWorldSpace(Vector2 guiPos, float screenDepth)
    {
        var sceneCam = UnityEditor.SceneView.lastActiveSceneView.camera;
        Vector3 screen_pos = GuiSpaceToScreenSpace(guiPos);
        screen_pos.z = screenDepth;
        return sceneCam.ScreenToWorldPoint(screen_pos);
    }
    private static Vector2 ScreenSpaceToGuiSpace(Vector2 screenPos)
    {
        return new Vector2(screenPos.x, Camera.current.pixelHeight - screenPos.y);
    }
    private static Vector2 GuiSpaceToScreenSpace(Vector2 guiPos)
    {
        return new Vector2(guiPos.x, Camera.current.pixelHeight - guiPos.y);
    }
    #endregion
    public enum PointType
    {
        Position = 0,
        PlusButton = 1,
        SplitPoint = 2,
        ValuePoint = 3,
        ValuePointLeftTangent = 4,
        ValuePointRightTangent = 5,
    }
    public static PGIndex PointTypeToPGIndex(PointType type)
    {
        switch (type){
            case PointType.ValuePoint:
                return PGIndex.Position;
            case PointType.ValuePointLeftTangent:
                return PGIndex.LeftTangent;
            case PointType.ValuePointRightTangent:
                return PGIndex.RightTangent;
            default:
                throw new ArgumentException();
        }
    }
    private class PointInfo
    {
        public PointType type;
        public Color color;
        public Texture2D texture;
        public Vector3 worldPosition;
        public Vector2 guiPos;
        public float screenDepth;
        public bool isPointOnScreen;
        public float distanceToMouse;
        public int indexInList;
        public int dataIndex;
        public PointInfo(Vector3 worldPosition, Color color,Texture2D texture,int indexInList,PointType type,int dataIndex=-1)
        {
            var MousePos = Event.current.mousePosition;
            this.color = color;
            this.worldPosition = worldPosition; 
            this.texture = texture;
            this.indexInList = indexInList;
            this.type = type;
            this.dataIndex = dataIndex;
            this.isPointOnScreen = WorldToGUISpace(worldPosition, out this.guiPos, out this.screenDepth);
            distanceToMouse = Vector2.Distance(this.guiPos,MousePos);
        }
    }
    private const float buttonClickDistance=20.0f;
    private const float lineClickDistance=15.0f;
    private const int plusButtonDistance = 30;
    public static void EditBezierCurve(Curve3D curve)
    {
        curve.TryInitialize();
        var positionCurve = curve.positionCurve;

        var sizeCurve = curve.curveSizeAnimationCurve;

        int controlID = GUIUtility.GetControlID(_BeizerHint, FocusType.Passive);
        var MousePos = Event.current.mousePosition;

        if (positionCurve.isCurveOutOfDate && GUIUtility.hotControl!=controlID)//we only cache when NOT moving the point
        {
            positionCurve.isCurveOutOfDate = false;
            positionCurve.CacheSampleCurve();
        }

        List<PointInfo> points = null;
        PointInfo hotPoint;
        PointInfo recentlySelectedPoint;
        Texture2D linePlaceTexture;
        CurveSplitPointInfo curveSplitPoint=null;
        PointInfo GetClosestPointToMouse()
        {
            float minDistanceToMouse = buttonClickDistance;
            PointInfo closestPoint = null;
            foreach (var i in points)
            {
                if (i.distanceToMouse<minDistanceToMouse)
                {
                    closestPoint = i;
                    minDistanceToMouse = i.distanceToMouse;
                }
            }
            return closestPoint;
        }

        void PopulatePoints(){
            points = new List<PointInfo>();
            hotPoint = null;
            recentlySelectedPoint = null;
            linePlaceTexture = null;
            curveSplitPoint = null;
            switch (curve.editMode)
            {
                case EditMode.PositionCurve:
                    #region PositionCurve
                    linePlaceTexture = curve.circleIcon;
                    for (int i = 0; i < positionCurve.NumControlPoints; i++)
                    {
                        var pointIndexType = BeizerCurve.GetPointTypeByIndex(i);
                        bool isPositionPoint = pointIndexType == PGIndex.Position;
                        var color = Color.green;
                        var tex = isPositionPoint ? curve.circleIcon : curve.squareIcon;
                        points.Add(new PointInfo(curve.transform.TransformPoint(positionCurve[i]), color, tex, i,PointType.Position));
                    }
                    PointInfo pointInfo;
                    if (positionCurve.NumControlPoints == 0)
                    {
                        pointInfo = new PointInfo(curve.transform.position, Color.blue, curve.plusButton, -1,PointType.PlusButton);
                    }
                    else
                    {
                        int count = positionCurve.NumControlPoints;
                        var lastPoint = positionCurve[count - 1];
                        var secondToLastPoint = positionCurve[count - 2];
                        var plusButtonVector = plusButtonDistance*(lastPoint - secondToLastPoint).normalized;
                        pointInfo = new PointInfo(curve.transform.TransformPoint(plusButtonVector+lastPoint), Color.blue, curve.plusButton, -1,PointType.PlusButton);
                    }
                    points.Add(pointInfo);
                    break;
                #endregion
                case EditMode.Size:
                    #region size
                    if (positionCurve.NumControlPoints < 4)
                        break;
                    linePlaceTexture = curve.diamondIcon;
                    var sizeKeys = sizeCurve.keys;
                    int c = 0;
                    int pointIndex = 0;
                    var leftWorldVector = (positionCurve[1]-positionCurve[0]).normalized;
                    var rightWorldVector= (positionCurve[positionCurve.NumControlPoints-1]-positionCurve[positionCurve.NumControlPoints-2]).normalized;
                    var curveStartPoint = positionCurve[0];
                    var curveEndPoint = positionCurve[positionCurve.NumControlPoints-1];
                    positionCurve.CacheLengths();//see if we can comment this out
                    foreach (var key in sizeKeys)
                    {
                        float progressAlongSegment;
                        int segmentIndex2 = positionCurve.GetSegmentIndexAndTimeByDistance(key.time, out progressAlongSegment);
                        var keyframeInfo = new KeyframeInfo(key, segmentIndex2, progressAlongSegment);
                        if (c > 0)
                        {
                            var segmentDistance = key.time - sizeCurve.keys[c - 1].time;
                            var leftXDistance = (key.inWeight * segmentDistance);
                            var leftTime = key.time - leftXDistance;
                            segmentIndex2 = positionCurve.GetSegmentIndexAndTimeByDistance(leftTime, out progressAlongSegment);
                            keyframeInfo.leftTangentProgressAlongSegment = progressAlongSegment;
                            keyframeInfo.leftTangentIndex = segmentIndex2;
                            keyframeInfo.leftTangentValue = key.value - leftXDistance * key.inTangent;
                            var pointInfo2 = new PointInfo(curve.transform.TransformPoint(positionCurve.GetPositionAtDistance(leftTime)), Color.magenta, curve.diamondIcon, pointIndex, PointType.ValuePointLeftTangent,c);
                            pointIndex++;
                            points.Add(pointInfo2);
                        }
                        if (key.time<0)
                        {
                            pointInfo = new PointInfo(curve.transform.TransformPoint(curveStartPoint + leftWorldVector * key.time), Color.magenta, linePlaceTexture, pointIndex, PointType.ValuePoint, c);
                        }
                        else if (key.time > positionCurve.GetLength())
                        {
                            pointInfo = new PointInfo(curve.transform.TransformPoint(curveEndPoint + rightWorldVector * (key.time - positionCurve.GetLength())), Color.magenta, linePlaceTexture, pointIndex, PointType.ValuePoint, c);
                        }
                        else
                        {
                            pointInfo = new PointInfo(curve.transform.TransformPoint(positionCurve.GetPositionAtDistance(key.time)), Color.magenta, linePlaceTexture, pointIndex, PointType.ValuePoint, c);
                        }
                        pointIndex++;
                        points.Add(pointInfo);
                        if (c < sizeCurve.keys.Length - 1)
                        {
                            var segmentDistance = sizeCurve.keys[c + 1].time - key.time;
                            var rightXDistance = (key.outWeight * segmentDistance);
                            var rightTime = key.time + rightXDistance;
                            segmentIndex2 = positionCurve.GetSegmentIndexAndTimeByDistance(rightTime, out progressAlongSegment);
                            keyframeInfo.rightTangentProgressAlongSegment = progressAlongSegment;
                            keyframeInfo.rightTangentIndex = segmentIndex2;
                            keyframeInfo.rightTangentValue = key.value + rightXDistance * key.outTangent;
                            var pointInfo2 = new PointInfo(curve.transform.TransformPoint(positionCurve.GetPositionAtDistance(rightTime)), Color.magenta, curve.diamondIcon, pointIndex, PointType.ValuePointRightTangent,c);
                            pointIndex++;
                            points.Add(pointInfo2);
                        }
                        c++;
                    }
                    break;
                #endregion
                case EditMode.Rotation:
                    #region Rotation curve
                    linePlaceTexture = curve.diamondIcon;
                    break;
                #endregion
                default:
                    throw new System.InvalidOperationException();
            }

            #region find split point
            var samples = positionCurve.GetCachedSampled();
            foreach (var i in samples)
            {
                i.position = curve.transform.TransformPoint(i.position);
            }
            int segmentIndex;
            float time;
            UnitySourceScripts.ClosestPointToPolyLine(out segmentIndex, out time, samples);
            foreach (var i in samples)
            {
                i.position = curve.transform.InverseTransformPoint(i.position);
            }
            curveSplitPoint = new CurveSplitPointInfo(segmentIndex, time);
            #endregion

            if (curve.IsAPointSelected)
            {
                hotPoint = points[curve.hotPointIndex];
            }
            else
            {
                hotPoint = GetClosestPointToMouse();
                if (hotPoint == null)
                {
                    Vector3 pointPosition = curve.transform.TransformPoint(positionCurve.GetSegmentPositionAtTime(segmentIndex, time));
                    var pointInfo = new PointInfo(pointPosition, Color.green, linePlaceTexture, -1,PointType.SplitPoint);
                    if (pointInfo.isPointOnScreen && pointInfo.distanceToMouse < lineClickDistance)
                    {
                        points.Add(pointInfo);
                        hotPoint = pointInfo;
                    }
                }
            }
        }
        PopulatePoints();

        bool isMainMouseButton = Event.current.button == 0;
        bool isCtrlPressed = Event.current.control;

        if (!MeshGenerator.IsBuzy)
        {
            if (curve.lastMeshUpdateEndTime != MeshGenerator.lastUpdateTime)
            {
                if (curve.mesh == null)
                {
                    curve.mesh = new Mesh();
                    curve.filter.mesh = curve.mesh;
                } else
                {
                    curve.mesh.Clear();
                }
                curve.mesh.SetVertices(MeshGenerator.vertices);
                curve.mesh.SetTriangles(MeshGenerator.triangles,0);
                curve.mesh.RecalculateNormals();
                curve.lastMeshUpdateEndTime = MeshGenerator.lastUpdateTime;
            }
            if (curve.lastMeshUpdateStartTime != MeshGenerator.lastUpdateTime)
            {
                MeshGenerator.StartGenerating(curve);
            }
        }

        if (curve.HaveCurveSettingsChanged())
            curve.lastMeshUpdateStartTime = DateTime.Now;

        void OnUndo()
        {
            curve.lastMeshUpdateStartTime = DateTime.Now;
            positionCurve.CacheSampleCurve();
            //Debug.Log("undo!");
            //curve.positionCurve.CacheLengths();
        }

        
        //TODO this is bad
        Undo.undoRedoPerformed = null;
        Undo.undoRedoPerformed += OnUndo;

        int InsertKeyframeAtSplitPoint(AnimationCurve targetCurve,Keyframe? frame=null)
        {
            Keyframe[] keys = targetCurve.keys;
            float pointDistanceAlongCurve = GetSplitPointDistance();
            int insertionIndex = 0;
            {
                if (pointDistanceAlongCurve >= keys[keys.Length - 1].time)
                {
                    insertionIndex = keys.Length;
                }
                else if (pointDistanceAlongCurve < keys[0].time)
                {
                    insertionIndex = 0;
                }
                else
                {
                    for (int i = 0; i < keys.Length; i++)
                    {
                        if (pointDistanceAlongCurve < keys[i].time)
                        {
                            insertionIndex = i;
                            break;
                        }
                    }
                }
            }
            Keyframe[] newKeys = new Keyframe[keys.Length + 1];
            for (int i = 0; i < newKeys.Length; i++)
            {
                if (i < insertionIndex)
                {
                    newKeys[i] = keys[i];
                }
                else if (i > insertionIndex)
                {
                    newKeys[i] = keys[i - 1];
                }
                else
                {
                    Keyframe insertedKeyframe;
                    if (frame.HasValue)
                    {
                        insertedKeyframe = frame.Value;
                        insertedKeyframe.time = pointDistanceAlongCurve;
                    } else
                    {
                        //Need to recalculate tangents here
                        insertedKeyframe= new Keyframe(pointDistanceAlongCurve, targetCurve.Evaluate(pointDistanceAlongCurve));
                    }
                    newKeys[i] = insertedKeyframe;
                }
            }
            targetCurve.keys = newKeys;
            return insertionIndex;
        }
        
        Keyframe RemoveKeyframe(AnimationCurve targetCurve, int index)
        {
            var oldKeys = targetCurve.keys;
            Keyframe[] newKeys = new Keyframe[oldKeys.Length-1];
            Keyframe retr = oldKeys[oldKeys.Length - 1];
            for (int i = 0; i < oldKeys.Length; i++)
            {
                if (i < index)
                    newKeys[i] = oldKeys[i];
                else if (i > index)
                    newKeys[i-1] = oldKeys[i];
                else
                    retr = oldKeys[i];
            }
            targetCurve.keys = newKeys;
            return retr;
        }

        float GetSplitPointDistance()
        {
            return positionCurve.GetDistanceBySegmentIndexAndTime(curveSplitPoint.segmentIndex, curveSplitPoint.time);
        }

        void OnDrag()
        {
            if (hotPoint != null && GUIUtility.hotControl == controlID && isMainMouseButton)
            {
                switch (curve.editMode)
                {
                    case EditMode.PositionCurve:
                        var oldPointPosition = positionCurve[hotPoint.indexInList];
                        var newPointPosition = curve.transform.InverseTransformPoint(GUIToWorldSpace(MousePos + curve.pointDragOffset, hotPoint.screenDepth));

                        #region update size curve
                        //Build keyframe info 
                        List<KeyframeInfo> keyframes = new List<KeyframeInfo>(sizeCurve.keys.Length);
                        var modifiedPointType = BeizerCurve.GetPointTypeByIndex(hotPoint.indexInList);
                        var pointGroup = positionCurve.GetPointGroupByIndex(hotPoint.indexInList);
                        var pointGroupIndex = BeizerCurve.GetPointGroupIndex(hotPoint.indexInList);
                        for (int i = 0; i < sizeCurve.keys.Length; i++)
                        {
                            var key = sizeCurve.keys[i];
                            float progressAlongSegment;
                            int segmentIndex = positionCurve.GetSegmentIndexAndTimeByDistance(key.time, out progressAlongSegment);
                            var keyframeInfo = new KeyframeInfo(key, segmentIndex, progressAlongSegment);
                            if (i > 0)
                            {
                                segmentIndex = positionCurve.GetSegmentIndexAndTimeByDistance(sizeCurve.GetKeyframeX(i,PGIndex.LeftTangent), out progressAlongSegment);
                                keyframeInfo.leftTangentProgressAlongSegment = progressAlongSegment;
                                keyframeInfo.leftTangentIndex = segmentIndex;
                                keyframeInfo.leftTangentValue = sizeCurve.GetKeyframeY(i, PGIndex.LeftTangent);
                            }
                            if (i < sizeCurve.keys.Length - 1)
                            {
                                segmentIndex = positionCurve.GetSegmentIndexAndTimeByDistance(sizeCurve.GetKeyframeX(i,PGIndex.RightTangent), out progressAlongSegment);
                                keyframeInfo.rightTangentProgressAlongSegment = progressAlongSegment;
                                keyframeInfo.rightTangentIndex = segmentIndex;
                                keyframeInfo.rightTangentValue = sizeCurve.GetKeyframeY(i, PGIndex.RightTangent);
                            }
                            keyframes.Add(keyframeInfo);
                        }

                        //////Actually update the point's position///////
                        positionCurve[hotPoint.indexInList] = newPointPosition;
                        /////////////////////////////////////////////////

                        {//Update curve length
                            bool lowerShouldRecalculateLength = pointGroup.DoesEditAffectBothSegments(modifiedPointType);
                            bool upperShouldRecalculateLength = pointGroup.DoesEditAffectBothSegments(modifiedPointType);
                            if (pointGroupIndex == 0)
                                lowerShouldRecalculateLength = false;
                            else if (pointGroupIndex == positionCurve.NumSegments)
                                upperShouldRecalculateLength = false;
                            if (lowerShouldRecalculateLength)
                                positionCurve.CacheSegmentLength(pointGroupIndex - 1);
                            if (upperShouldRecalculateLength)
                                positionCurve.CacheSegmentLength(pointGroupIndex);
                        }

                        {
                            Keyframe[] keys = sizeCurve.keys;
                            for (int i = 0; i < keyframes.Count; i++)
                            {
                                var key = sizeCurve.keys[i];
                                var data = keyframes[i];
                                key.weightedMode = WeightedMode.Both;
                                key.time = positionCurve.GetDistanceBySegmentIndexAndTime(data.segmentIndex, data.progressAlongSegment);
                                keys[i] = key;
                            }
                            sizeCurve.keys = keys;

                            for (int i = 0; i < keyframes.Count; i++)
                            {
                                var key = sizeCurve.keys[i];
                                var data = keyframes[i];
                                if (i > 0)
                                {
                                    var leftX = positionCurve.GetDistanceBySegmentIndexAndTime(data.leftTangentIndex, data.leftTangentProgressAlongSegment);
                                    var leftY = data.leftTangentValue;
                                    sizeCurve.SetKeyframeX(i,PGIndex.LeftTangent,leftX);
                                    sizeCurve.SetKeyframeY(i,PGIndex.LeftTangent,leftY);
                                }
                                if (i < sizeCurve.keys.Length - 1)
                                {
                                    var rightX = positionCurve.GetDistanceBySegmentIndexAndTime(data.rightTangentIndex, data.rightTangentProgressAlongSegment);
                                    var rightY = data.rightTangentValue;
                                    sizeCurve.SetKeyframeX(i, PGIndex.RightTangent, rightX);
                                    sizeCurve.SetKeyframeY(i, PGIndex.RightTangent,rightY);
                                }

                                keys[i] = key;
                            }
                            sizeCurve.keys = keys;
                        }

                        break;
                    #endregion

                    case EditMode.Size:
                        {
                            if (hotPoint.type == PointType.ValuePoint)
                            {
                                var key = RemoveKeyframe(sizeCurve,hotPoint.dataIndex);
                                var extrIndex = InsertKeyframeAtSplitPoint(sizeCurve, key);
                                var index = BeizerCurve.GetVirtualIndexByType(extrIndex, PointTypeToPGIndex(PointType.ValuePoint));
                                curve.hotPointIndex = index;
                                hotPoint.indexInList = index;
                            }
                            else if (hotPoint.type == PointType.ValuePointLeftTangent)
                            {
                                sizeCurve.SetKeyframeX(hotPoint.dataIndex, PGIndex.LeftTangent, GetSplitPointDistance());
                            }
                            else if (hotPoint.type == PointType.ValuePointRightTangent)
                            {
                                sizeCurve.SetKeyframeX(hotPoint.dataIndex, PGIndex.RightTangent, GetSplitPointDistance());
                            }
                        }
                        break;
                    default:
                        throw new System.InvalidOperationException();
                }
                positionCurve.isCurveOutOfDate = true;
                curve.lastMeshUpdateStartTime= DateTime.Now;
                Event.current.Use();
            }
        }
        switch (Event.current.GetTypeForControl(controlID))
        {
            case EventType.KeyDown:
                //Event.current.Use();
                break;
            case EventType.KeyUp:
                //Event.current.Use();
                break;
            case EventType.MouseDown:
                #region Mouse Down
                if (isMainMouseButton)
                {
                    if (hotPoint != null)
                    {
                        GUIUtility.hotControl = controlID;
                        curve.hotPointIndex = hotPoint.indexInList;
                        switch (curve.editMode)
                        {
                            case EditMode.PositionCurve:
                                if (hotPoint.type== PointType.SplitPoint)
                                {
                                    //Multiply by 3 because there are tangents filling up the list
                                    hotPoint.indexInList = 3*positionCurve.InsertSegmentAfterIndex(curveSplitPoint,positionCurve.placeLockedPoints,positionCurve.splitInsertionBehaviour);
                                    curve.hotPointIndex = hotPoint.indexInList;
                                    PopulatePoints();
                                    positionCurve.CacheLengths();
                                }
                                if (!isCtrlPressed)
                                {
                                    curve.selectedPointsIndex.Clear();
                                }
                                var currentIndexToSelect= BeizerCurve.GetParentVirtualIndex(hotPoint.indexInList);
                                curve.selectedPointsIndex.Add(currentIndexToSelect);
                                break;
                            case EditMode.Size:
                                if (hotPoint.type == PointType.SplitPoint)
                                {
                                    var extraIndex = InsertKeyframeAtSplitPoint(sizeCurve);
                                    hotPoint.dataIndex = extraIndex;
                                    hotPoint.type = PointType.ValuePoint;//We've inserted a point, so now we are editing a value point

                                    var index = BeizerCurve.GetVirtualIndexByType(extraIndex,PointTypeToPGIndex(PointType.ValuePoint));
                                    curve.hotPointIndex = index;
                                    hotPoint.indexInList = index;
                                }
                                break;
                            default:
                                throw new System.InvalidOperationException();
                        }
                        curve.pointDragOffset = hotPoint.guiPos - MousePos;
                        OnDrag();
                    }
                }
                break;
            #endregion
            case EventType.MouseDrag:
                OnDrag();
                break;
            case EventType.MouseUp:
                #region Mouse Up
                if (isMainMouseButton)
                {
                    if (hotPoint != null || GUIUtility.hotControl == controlID)
                    {
                        GUIUtility.hotControl = 0;
                        curve.hotPointIndex = -1;
                        Event.current.Use();
                    }
                }
                break;
                #endregion
            case EventType.MouseMove:
                HandleUtility.Repaint();
                break;
            case EventType.Repaint:
                #region repaint

                void DrawCurveFromIndex(int index,Texture2D tex,BeizerCurve pointProvider)
                {
                    var point1 = curve.transform.TransformPoint(pointProvider[index, 0]);
                    var point2 = curve.transform.TransformPoint(pointProvider[index, 3]);
                    var tangent1 = curve.transform.TransformPoint(pointProvider[index, 1]);
                    var tangent2 = curve.transform.TransformPoint(pointProvider[index, 2]);
                    Handles.DrawBezier(point1, point2, tangent1, tangent2, new Color(.8f, .8f, .8f), tex, 10);
                }
                for (int i = 0; i < positionCurve.NumSegments; i++)
                {
                    DrawCurveFromIndex(i,curve.lineTex,positionCurve);
                }
                if (curve.editMode == EditMode.Size)
                {
                    for (int c = 0; c < sizeCurve.keys.Length; c++)
                    {
                        var clonedCurve = new BeizerCurve(positionCurve);

                        bool drawLeft = c > 0;
                        float leftTime;
                        int leftSegmentIndex = -1;
                        if (drawLeft)
                        {
                            leftSegmentIndex = clonedCurve.GetSegmentIndexAndTimeByDistance(sizeCurve.GetKeyframeX(c,PGIndex.LeftTangent),out leftTime);
                            leftSegmentIndex = clonedCurve.InsertSegmentAfterIndex(new CurveSplitPointInfo(leftSegmentIndex, leftTime), false, BeizerCurve.SplitInsertionNeighborModification.RetainCurveShape);
                            clonedCurve.CacheSegmentLength(leftSegmentIndex,true);
                            clonedCurve.CacheSegmentLength(leftSegmentIndex-1,false);
                        }
                        float centerTime;
                        var centerDist = sizeCurve.GetKeyframeX(c, PGIndex.Position);
                        var centerSegmentIndex = clonedCurve.GetSegmentIndexAndTimeByDistance(centerDist,out centerTime);
                        centerSegmentIndex = clonedCurve.InsertSegmentAfterIndex(new CurveSplitPointInfo(centerSegmentIndex, centerTime), false, BeizerCurve.SplitInsertionNeighborModification.RetainCurveShape);
                        clonedCurve.CacheSegmentLength(centerSegmentIndex,true);
                        clonedCurve.CacheSegmentLength(centerSegmentIndex-1,false);
                        var newCenterDist = clonedCurve.GetDistanceBySegmentIndexAndTime(centerSegmentIndex,0);
                        Vector2 pos;
                        float depth;
                        Handles.BeginGUI();
                        if (WorldToGUISpace(clonedCurve.GetPositionAtDistance(newCenterDist),out pos,out depth))
                            DrawPoint(GetRectCenteredAtPosition(pos,6,6),Color.red,curve.squareIcon);
                        Handles.EndGUI();
                        if (drawLeft)
                        {
                            for (int i = leftSegmentIndex; i < centerSegmentIndex; i++)
                                DrawCurveFromIndex(i,curve.leftTangentLineTex,clonedCurve);
                        }
                        
                        if (c <sizeCurve.keys.Length-1)
                        {
                            float rightTime;
                            int rightSegmentIndex= clonedCurve.GetSegmentIndexAndTimeByDistance(sizeCurve.GetKeyframeX(c,PGIndex.RightTangent),out rightTime);
                            rightSegmentIndex = clonedCurve.InsertSegmentAfterIndex(new CurveSplitPointInfo(rightSegmentIndex, rightTime), false, BeizerCurve.SplitInsertionNeighborModification.RetainCurveShape);
                        }
                    }
                }

                switch (curve.editMode)
                {
                    case EditMode.PositionCurve:
                        #region PositionCurve
                        foreach (var i in positionCurve.PointGroups)
                        {
                            if (i.hasLeftTangent)
                                Handles.DrawAAPolyLine(curve.lineTex, new Vector3[2] { curve.transform.TransformPoint(i.GetWorldPositionByIndex(PGIndex.LeftTangent)), curve.transform.TransformPoint(i.GetWorldPositionByIndex(PGIndex.Position))});
                            if (i.hasRightTangent)
                                Handles.DrawAAPolyLine(curve.lineTex, new Vector3[2] { curve.transform.TransformPoint(i.GetWorldPositionByIndex(PGIndex.Position)), curve.transform.TransformPoint(i.GetWorldPositionByIndex(PGIndex.RightTangent)) });
                        }
                        /*draw curve debug line
                        var curveLen = positionCurve.GetLength();
                        float time;
                        var prevPoint = positionCurve.GetPositionAtDistance(0,out time);
                        for (int i = 0; i < 100; i++)
                        {
                            var currPoint = positionCurve.GetPositionAtDistance((i/100.0f)*curveLen,out time);
                            Debug.DrawLine(prevPoint, currPoint);
                            prevPoint = currPoint;    
                        }
                        */
                        if (curve.IsAPointSelected)
                        {
                            hotPoint.color = Color.yellow;
                            var renderPoint = points[curve.hotPointIndex];
                            var pointGroup = positionCurve.GetPointGroupByIndex(renderPoint.indexInList);
                            if (pointGroup.GetIsPointLocked())
                            {
                                switch (BeizerCurve.GetPointTypeByIndex(hotPoint.indexInList))
                                {
                                    case PGIndex.Position:
                                        break;
                                    case PGIndex.LeftTangent:
                                        if (pointGroup.hasRightTangent)
                                            points[curve.hotPointIndex + 2].color = Color.yellow;
                                        break;
                                    case PGIndex.RightTangent:
                                        if (pointGroup.hasLeftTangent)
                                            points[curve.hotPointIndex - 2].color = Color.yellow;
                                        break;
                                    default:
                                        throw new System.InvalidOperationException();
                                }
                            }
                            //var otherPoint = (int)positionCurve.GetOtherTangentIndex(positionCurve.GetPointTypeByIndex(hotPoint.index));
                        }
                        else
                        {
                            foreach (var recentlySelectedPointIndex in curve.selectedPointsIndex)
                            {
                                var renderPoint = points[recentlySelectedPointIndex];
                                var pointGroup = positionCurve.GetPointGroupByIndex(renderPoint.indexInList);
                                renderPoint.color = Color.yellow;
                                if (pointGroup.hasLeftTangent)
                                    points[recentlySelectedPointIndex - 1].color = Color.yellow;
                                if (pointGroup.hasRightTangent)
                                    points[recentlySelectedPointIndex + 1].color = Color.yellow;
                            }
                        }
                        break;
                    #endregion
                    case EditMode.Size:
                        break;
                    default:
                        throw new System.InvalidOperationException();
                }

                if (hotPoint != null && !curve.IsAPointSelected)
                {
                    hotPoint.color *= .7f;
                    hotPoint.color.a = 1;
                }
                Handles.BeginGUI();

                points.RemoveAll(a => !a.isPointOnScreen);
                points.Sort((a, b) => (int)Mathf.Sign(b.screenDepth - a.screenDepth));
                foreach (var i in points)
                {
                    DrawPoint(GetRectCenteredAtPosition(i.guiPos,6,6),i.color,i.texture);
                }

                Handles.EndGUI();

                break;
                #endregion
        }
    }
}

