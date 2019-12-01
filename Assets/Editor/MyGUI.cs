using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class MyGUI
{
    private static readonly int _BeizerHint = "MyGUI.Beizer".GetHashCode();

    private const int _pointHitboxSize = 10;

    #region gui tools
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

    private struct PointRenderInfo
    {
        public float screenDepth;
        public Color color;
        public Texture2D texture;
        public Rect rect;
        public PointRenderInfo(float screenDepth, Rect rect, Color color,Texture2D texture)
        {
            this.screenDepth = screenDepth;
            this.color = color;
            this.rect = rect;
            this.texture = texture;
        }
    }
    public static void EditBezierCurve(Curve3D curve,Vector3 position)
    {
        int controlID = GUIUtility.GetControlID(_BeizerHint, FocusType.Passive);
        var MousePos = Event.current.mousePosition;
        switch (Event.current.GetTypeForControl(controlID))
        {
            case EventType.MouseDown:
                break;
            case EventType.MouseDrag:
                break;
            case EventType.MouseUp:
                break;
            case EventType.MouseMove:
                Event.current.Use();
                break;
            case EventType.Repaint:
                for (int i=0;i<curve.curve.NumSegments;i++)
                {
                    var point1 = curve.curve[i, 0] + position;
                    var point2 = curve.curve[i, 3] + position;
                    var tangent1 = curve.curve[i, 1] + position;
                    var tangent2 = curve.curve[i, 2] + position;
                    Handles.DrawBezier(point1,point2,tangent1,tangent2,new Color(.8f,.8f,.8f),curve.lineTex,10);
                }
                foreach (var i in curve.curve.PointGroups)
                {
                    if (i.hasLeftTangent)
                        Handles.DrawAAPolyLine(curve.lineTex,new Vector3[2] { i.GetWorldPositionByIndex(PGIndex.LeftTangent) + position, i.GetWorldPositionByIndex(PGIndex.Position) + position });
                    if (i.hasRightTangent)
                        Handles.DrawAAPolyLine(curve.lineTex,new Vector3[2] { i.GetWorldPositionByIndex(PGIndex.Position) + position, i.GetWorldPositionByIndex(PGIndex.RightTangent) + position });
                }
                Handles.BeginGUI();

                Color DesaturateColor(Color color, float amount)
                {
                    return Color.Lerp(color,Color.white,amount);
                }
                List<PointRenderInfo> pointsToDraw = new List<PointRenderInfo>();
                for (int i = 0; i < curve.curve.NumControlPoints; i++)
                {
                    Vector2 guiPos;
                    float screenDepth;
                    if (WorldToGUISpace(curve.curve[i]+position, out guiPos, out screenDepth))
                    {
                        bool isPrimaryPoint = curve.curve.GetPointTypeByIndex(i) == PGIndex.Position;
                        var color = curve.curve.GetPointGroupByIndex(i).GetIsPointLocked() ? Color.red : Color.green;
                        float colorLerper = isPrimaryPoint?0.0f:.65f;
                        var tex = isPrimaryPoint ?curve.circleIcon:curve.squareIcon;
                        pointsToDraw.Add(new PointRenderInfo(screenDepth,GetRectCenteredAtPosition(guiPos,6,6),DesaturateColor(color,colorLerper),tex));
                    }
                }
                pointsToDraw.Sort((a,b)=>(int)Mathf.Sign(b.screenDepth-a.screenDepth));
                foreach (var i in pointsToDraw)
                {
                    DrawPoint(i.rect,i.color,i.texture);
                }
                Handles.EndGUI();
                /*curve.curve.CacheLengths();
                var samples = curve.curve.SampleCurve(curve.sampleRate).ToArray();
                for(int i = 0; i < samples.Length; i++) {
                    samples[i] += position;//+new Vector3(0,Random.value,0);
                }
                if (samples.Length >= 2)
                {
                    var pos = HandleUtility.ClosestPointToPolyLine(samples);
                    Handles.DrawRectangle(controlID, pos, Quaternion.identity, .2f);
                    Handles.DrawPolyLine(samples);
                }*/
                break; 
        }
    }


    /*private static readonly GUIContent s_TempContent = new GUIContent();

    private static GUIContent TempContent(string text)
    {
        s_TempContent.text = text;
        s_TempContent.image = null;
        s_TempContent.tooltip = null;
        return s_TempContent;
    }


    private static readonly int s_ButtonHint = "MyGUI.Button".GetHashCode();

    public static bool Button(Rect position, GUIContent label, GUIStyle style)
    {
        int controlID = GUIUtility.GetControlID(s_ButtonHint, FocusType.Passive, position);
        bool result = false;

        switch (Event.current.GetTypeForControl(controlID))
        {
            case EventType.MouseDown:
                if (GUI.enabled && position.Contains(Event.current.mousePosition))
                {
                    GUIUtility.hotControl = controlID;
                    Event.current.Use();
                }
                break;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl == controlID)
                {
                    Event.current.Use();
                }
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl == controlID)
                {
                    GUIUtility.hotControl = 0;

                    if (position.Contains(Event.current.mousePosition))
                    {
                        result = true;
                        Event.current.Use();
                    }
                }
                break;

            case EventType.KeyDown:
                if (GUIUtility.hotControl == controlID)
                {
                    if (Event.current.keyCode == KeyCode.Escape)
                    {
                        GUIUtility.hotControl = 0;
                        Event.current.Use();
                    }
                }
                break;

            case EventType.Repaint:
                //Draw says you GUIStyle, you draw this GUIContent
                style.Draw(position, label, controlID);
                break;
        }

        return result;
    }

    public static bool Button(Rect position, GUIContent label)
    {
        return Button(position, label, GUI.skin.button);
    }

    public static bool Button(Rect position, string label, GUIStyle style)
    {
        return Button(position, TempContent(label), style);
    }

    public static bool Button(Rect position, string label)
    {
        return Button(position, label, GUI.skin.button);
    }


    // Button Control - Layout Version

    public static bool Button(GUIContent label, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect position = GUILayoutUtility.GetRect(label, style, options);
        return Button(position, label, style);
    }

    public static bool Button(GUIContent label, params GUILayoutOption[] options)
    {
        return Button(label, GUI.skin.button, options);
    }

    public static bool Button(string label, GUIStyle style, params GUILayoutOption[] options)
    {
        return Button(TempContent(label), style, options);
    }

    public static bool Button(string label, params GUILayoutOption[] options)
    {
        return Button(label, GUI.skin.button, options);
    }*/
}

