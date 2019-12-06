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
    private class PointInfo
    {
        public Color color;
        public Texture2D texture;
        public Vector3 worldPosition;
        public Vector2 guiPos;
        public float screenDepth;
        public bool isPointOnScreen;
        public float distanceToMouse;
        public int index;
        public PointInfo(Vector3 worldPosition, Color color,Texture2D texture,int index)
        {
            var MousePos = Event.current.mousePosition;
            this.color = color;
            this.worldPosition = worldPosition; 
            this.texture = texture;
            this.index = index;
            this.isPointOnScreen = WorldToGUISpace(worldPosition, out this.guiPos, out this.screenDepth);
            distanceToMouse = Vector2.Distance(this.guiPos,MousePos);
        }
    }
    private const float buttonClickDistance=20.0f;
    private const float lineClickDistance=15.0f;
    public static void EditBezierCurve(Curve3D curve,Vector3 position)

    {
        if (curve.positionCurve == null)
            curve.positionCurve.Initialize();

        int controlID = GUIUtility.GetControlID(_BeizerHint, FocusType.Passive);
        var MousePos = Event.current.mousePosition;

        if (curve.positionCurve.isCurveOutOfDate && GUIUtility.hotControl!=controlID)
        {
            curve.positionCurve.isCurveOutOfDate = false;
            Debug.Log("cached!");
            curve.positionCurve.CacheSampleCurve();
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
                    for (int i = 0; i < curve.positionCurve.NumControlPoints; i++)
                    {
                        var pointIndexType = curve.positionCurve.GetPointTypeByIndex(i);
                        bool isPositionPoint = pointIndexType == PGIndex.Position;
                        var color = Color.green;
                        var tex = isPositionPoint ? curve.circleIcon : curve.squareIcon;
                        points.Add(new PointInfo(curve.positionCurve[i] + position, color, tex, i));
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
            if (curve.IsAPointSelected)
            {
                hotPoint = points[curve.hotPointIndex];
            }
            else
            {
                hotPoint = GetClosestPointToMouse();
                if (hotPoint == null)
                {
                    var samples = curve.positionCurve.GetCachedSampled();
                    foreach (var i in samples)
                    {
                        i.position += position;
                    }
                    int segmentIndex;
                    float time;
                    UnitySourceScripts.ClosestPointToPolyLine(out segmentIndex, out time, samples);
                    foreach (var i in samples)
                    {
                        i.position -= position;
                    }
                    Vector3 pointPosition = curve.positionCurve.GetSegmentPositionAtTime(segmentIndex, time) + position;
                    curveSplitPoint = new CurveSplitPointInfo(segmentIndex,time);
                    var pointInfo = new PointInfo(pointPosition, Color.green, linePlaceTexture, -1);
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
        void OnDrag()
        {
            if (hotPoint != null && GUIUtility.hotControl == controlID && isMainMouseButton)
            {
                switch (curve.editMode)
                {
                    case EditMode.PositionCurve:
                        curve.positionCurve[hotPoint.index] = GUIToWorldSpace(MousePos + curve.pointDragOffset, hotPoint.screenDepth) - position;
                        curve.positionCurve.isCurveOutOfDate = true;
                        break;
                    default:
                        throw new System.InvalidOperationException();
                }
                Event.current.Use();
            }
        }

        switch (Event.current.GetTypeForControl(controlID))
        {
            case EventType.MouseDown:
                if (isMainMouseButton)
                {
                    if (hotPoint != null)
                    {
                        GUIUtility.hotControl = controlID;
                        curve.hotPointIndex = hotPoint.index;
                        switch (curve.editMode)
                        {
                            case EditMode.PositionCurve:
                                if (curveSplitPoint!=null)
                                {
                                    hotPoint.index = curve.positionCurve.InsertSegmentAfterIndex(curveSplitPoint);
                                    curve.hotPointIndex = hotPoint.index;
                                    PopulatePoints();
                                }
                                curve.recentlySelectedPointIndex = curve.positionCurve.GetParentVirtualIndex(hotPoint.index);
                                break;
                            default:
                                throw new System.InvalidOperationException();
                        }
                        curve.pointDragOffset = hotPoint.guiPos - MousePos;
                        OnDrag();
                    }
                }
                break;
            case EventType.MouseDrag:
                OnDrag();
                break;
            case EventType.MouseUp:
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
            case EventType.MouseMove:
                HandleUtility.Repaint();
                break;
            case EventType.Repaint:
                #region repaint

                for (int i = 0; i < curve.positionCurve.NumSegments; i++)
                {
                    var point1 = curve.positionCurve[i, 0] + position;
                    var point2 = curve.positionCurve[i, 3] + position;
                    var tangent1 = curve.positionCurve[i, 1] + position;
                    var tangent2 = curve.positionCurve[i, 2] + position;
                    Handles.DrawBezier(point1, point2, tangent1, tangent2, new Color(.8f, .8f, .8f), curve.lineTex, 10);
                }

                switch (curve.editMode)
                {
                    case EditMode.PositionCurve:
                        #region PositionCurve
                        foreach (var i in curve.positionCurve.PointGroups)
                        {
                            if (i.hasLeftTangent)
                                Handles.DrawAAPolyLine(curve.lineTex, new Vector3[2] { i.GetWorldPositionByIndex(PGIndex.LeftTangent) + position, i.GetWorldPositionByIndex(PGIndex.Position) + position });
                            if (i.hasRightTangent)
                                Handles.DrawAAPolyLine(curve.lineTex, new Vector3[2] { i.GetWorldPositionByIndex(PGIndex.Position) + position, i.GetWorldPositionByIndex(PGIndex.RightTangent) + position });
                        }
                        if (curve.IsAPointSelected)
                        {
                            hotPoint.color = Color.yellow;
                            var renderPoint = points[curve.hotPointIndex];
                            var pointGroup = curve.positionCurve.GetPointGroupByIndex(renderPoint.index);
                            if (pointGroup.GetIsPointLocked())
                            {
                                switch (curve.positionCurve.GetPointTypeByIndex(hotPoint.index))
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
                            
                            //var otherPoint = (int)curve.positionCurve.GetOtherTangentIndex(curve.positionCurve.GetPointTypeByIndex(hotPoint.index));
                        }
                        else if (curve.recentlySelectedPointIndex!=-1)
                        {
                            var renderPoint = points[curve.recentlySelectedPointIndex];
                            var pointGroup = curve.positionCurve.GetPointGroupByIndex(renderPoint.index);
                            renderPoint.color = Color.yellow;
                            if (pointGroup.hasLeftTangent)
                                points[curve.recentlySelectedPointIndex - 1].color = Color.yellow;
                            if (pointGroup.hasRightTangent)
                                points[curve.recentlySelectedPointIndex + 1].color = Color.yellow;
                        }
                        break;
                    #endregion
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

