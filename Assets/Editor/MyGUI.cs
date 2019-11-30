using UnityEditor;
using UnityEngine;

public static class MyGUI
{
    private static readonly int _BeizerHint = "MyGUI.Beizer".GetHashCode();

    private const int _pointHitboxSize = 10;
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
                /*for (int i=0;i<curve.NumSegments;i++)
                {
                    Handles.DrawBezier(curve[i,0]+position, curve[i,3]+position, curve[i,1]+position, curve[i,2]+position,Color.white,Texture2D.whiteTexture,3);
                }*/
                curve.curve.CacheLengths();
                var samples = curve.curve.SampleCurve(curve.sampleRate).ToArray();
                for(int i = 0; i < samples.Length; i++) {
                    samples[i] += position;
                }
                if (samples.Length >= 2)
                {
                    var pos = HandleUtility.ClosestPointToPolyLine(samples);
                    Handles.DrawRectangle(controlID, pos, Quaternion.identity, .2f);
                    Handles.DrawPolyLine(samples);
                }
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

