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
            curve3d.UICurve = new CurveComposite(null,curve3d);//prob shouldn't do this every frame
        var curveEditor = curve3d.UICurve;
        var MousePos = Event.current.mousePosition;
        int controlID = GUIUtility.GetControlID(_CurveHint, FocusType.Passive);
        switch (Event.current.GetTypeForControl(controlID))
        {
            case EventType.Repaint:
                Draw(curveEditor,MousePos,elementClickedDown);
                break;
            case EventType.MouseDown:
                if (Event.current.button == 0)
                {
                    var clicked = GetClosestElementToCursor(curveEditor, MousePos);
                    if (clicked != null)
                    {
                        GUIUtility.hotControl = controlID;
                        curve3d.elementClickedDown = clicked;
                        var clickPos = MousePos + clicked.offset;
                        clicked.commandToExecute.ClickDown(clickPos);
                        clicked.commandToExecute.ClickDrag(clickPos, curve3d, clicked);
                        Event.current.Use();
                    }
                }
                break;
            case EventType.MouseDrag:
                if (elementClickedDown != null)
                {
                    var clickPos = MousePos + elementClickedDown.offset;
                    elementClickedDown.commandToExecute.ClickDrag(clickPos,curve3d,elementClickedDown);
                    Event.current.Use();
                }
                break;
            case EventType.MouseUp:
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
    private const float SmallClickRadius = 5;
    private const float LargeClickRadius = 20;
    ClickHitData GetClosestElementToCursor(IComposite root,Vector2 clickPosition)
    {
        ClickHitData GetHit(List<ClickHitData> hits)
        {
            var veryClosehits = hits.Where(a => a.distanceFromClick < SmallClickRadius).ToList();
            if (veryClosehits.Count > 0)
            {
                veryClosehits.Sort((a, b) => (int)Mathf.Sign(a.distanceFromCamera - b.distanceFromCamera));
                return veryClosehits[0];
            }
            else
            {
                var somewhatCloseHits = hits.Where(a => a.distanceFromClick < LargeClickRadius).ToList();
                if (somewhatCloseHits.Count > 0)
                {
                    somewhatCloseHits.Sort((a, b) => (int)Mathf.Sign(a.distanceFromClick - b.distanceFromClick));
                    return somewhatCloseHits[0];
                }
                return null;
            }
        }
        List<ClickHitData> hitsList = new List<ClickHitData>();
        root.Click(clickPosition, hitsList);

        var highPriorityHits = hitsList.Where(a=>!a.isLowPriority).ToList();
        var highPriorityItem = GetHit(highPriorityHits);
        if (highPriorityItem != null)
            return highPriorityItem;

        var lowPriorityHits = hitsList.Where(a=>a.isLowPriority).ToList();
        var lowPriorityItem = GetHit(lowPriorityHits);
        if (lowPriorityItem != null)
            return lowPriorityItem;

        return null;
    }

    void Draw(IComposite root,Vector2 mousePos,ClickHitData currentlyHeldDown)
    {
        ClickHitData closestElementToCursor = null;
        if (currentlyHeldDown==null)
            closestElementToCursor = GetClosestElementToCursor(root,mousePos);
        List<IDraw> draws = new List<IDraw>();
        root.Draw(draws,closestElementToCursor);
        draws.Sort((a, b) => (int)(Mathf.Sign(b.DistFromCamera() - a.DistFromCamera())));
        foreach (var draw in draws)
            if (draw.DistFromCamera() > 0)
            {
                if (closestElementToCursor != null && draw.Creator() == closestElementToCursor.owner)
                    draw.Draw(DrawMode.hovered);
                else if (currentlyHeldDown != null && draw.Creator() == currentlyHeldDown.owner)
                    draw.Draw(DrawMode.clicked);
                else
                    draw.Draw(DrawMode.normal);
            }
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
