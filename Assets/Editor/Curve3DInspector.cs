using Assets.NewUI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityObject = UnityEngine.Object;


[CustomEditor(typeof(Curve3D))]
public class Curve3DInspector : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }
    /*private void OnSceneGUI()
    {
        var curve = target as Curve3D;
        Undo.RecordObject(curve, "curve");
        MyGUI.EditBezierCurve(curve);
        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(20, 20, 150, 60));
        var rect = EditorGUILayout.BeginVertical();
        GUI.color = Color.yellow;
        GUI.Box(rect, GUIContent.none);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("Edit Curve");
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUI.color = Color.red;
        if (GUILayout.Button("Add"))
        {
            curve.positionCurve.AddDefaultSegment();
        }
        if (GUILayout.Button("Lock"))
        {
            curve.positionCurve.placeLockedPoints = !curve.positionCurve.placeLockedPoints;
        }
        if (GUILayout.Button("Clear"))
        {
            Debug.Log("cleared");
            curve.selectedPointsIndex.Clear();
            curve.hotPointIndex = -1;
            curve.positionCurve.Initialize();
            curve.positionCurve.isCurveOutOfDate = true;
        }
        //GUI.color = Color.white;
        //EditorGUI.CurveField(new Rect(0, 0, 20, 20),curve.curveSizeAnimationCurve);
        GUILayout.EndHorizontal();
        GUILayout.EndArea();

        Handles.EndGUI();

    }*/
    private static readonly int _CurveHint = "NewGUI.CURVE".GetHashCode();
    private void OnSceneGUI()
    {
        var curve3d = (target as Curve3D);
        Undo.RecordObject(curve3d, "curve");
        ClickHitData elementClickedDown = curve3d.elementClickedDown;
        Curve3DSettings.circleTexture = curve3d.circleIcon;
        Curve3DSettings.squareTexture = curve3d.squareIcon;
        Curve3DSettings.diamondTexture = curve3d.diamondIcon;
        Curve3DSettings.defaultLineTexture = curve3d.lineTex;
        if (curve3d.UICurve==null)
            curve3d.UICurve = new CurveComposite(curve3d);//prob shouldn't do this every frame
        var curveEditor = curve3d.UICurve;
        var MousePos = Event.current.mousePosition;
        int controlID = GUIUtility.GetControlID(_CurveHint, FocusType.Passive);
        switch (Event.current.GetTypeForControl(controlID))
        {
            case EventType.Repaint:
                Draw(curveEditor);
                break;
            case EventType.MouseDown:
                var clicked = GetClickedElement(curveEditor,MousePos);
                Debug.Log("down");
                if (clicked != null)
                {
                    GUIUtility.hotControl = controlID;
                    curve3d.elementClickedDown = clicked;
                    clicked.commandToExecute.ClickDown(MousePos);
                    clicked.commandToExecute.ClickDrag(MousePos,curve3d,clicked);
                    Event.current.Use();
                }
                break;
            case EventType.MouseDrag:
                Debug.Log("drag");
                if (elementClickedDown != null)
                {
                    elementClickedDown.commandToExecute.ClickDrag(MousePos,curve3d,elementClickedDown);
                    Event.current.Use();
                }
                break;
            case EventType.MouseUp:
                Debug.Log("up");
                if (elementClickedDown != null)
                {
                    GUIUtility.hotControl = 0;
                    elementClickedDown.commandToExecute.ClickUp(MousePos);
                    curve3d.elementClickedDown = null;
                    Event.current.Use();
                }
                break;
            case EventType.MouseMove:
                HandleUtility.Repaint();
                break;
        }
    }
    private const float ClickRadius = 10;
    ClickHitData GetClickedElement(IComposite root,Vector2 clickPosition)
    {
        List<ClickHitData> hits = new List<ClickHitData>();
        root.Click(clickPosition, hits);
        var reducedHits = hits.Where(a => a.distanceFromClick < ClickRadius).ToList();
        if (reducedHits.Count == 0)
            return null;
        reducedHits.Sort((a, b) => (int)Mathf.Sign(a.distanceFromCamera - b.distanceFromCamera));
        return reducedHits[0];
    }
    void Draw(IComposite root)
    {
        List<IDraw> draws = new List<IDraw>();
        root.Draw(draws);
        draws.Sort((a, b) => (int)(Mathf.Sign(b.DistFromCamera() - a.DistFromCamera())));
        foreach (var draw in draws)
            if (draw.DistFromCamera()>0)
                draw.Draw();
    }
}
/*
//we could dick around trying to draw our own lines/icons so that we can sort using the depth buffer, but I say we do that laterS
GL.PushMatrix();
curve3d.testmat.SetPass(0);
GL.LoadOrtho();
GL.Begin(GL.TRIANGLES);
GL.Color(Color.green);
GL.Vertex3(0, 0, -10f);
GL.Vertex3(1, 1, -10f);
GL.Vertex3(0, 1, -10f);
GL.Color(Color.blue);
GL.Vertex3(0, 0, -5f);
GL.Vertex3(1, 1, -5f);
GL.Vertex3(0, 1, -5f);
GL.End();
GL.PopMatrix();
*/
