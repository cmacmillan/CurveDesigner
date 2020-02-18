using Assets.NewUI;
using System.Collections;
using System.Collections.Generic;
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
    private void OnSceneGUI()
    {
        var curve3d = (target as Curve3D);
        Curve3DSettings.circleTexture = curve3d.circleIcon;
        Curve3DSettings.squareTexture = curve3d.squareIcon;
        Curve3DSettings.diamondTexture = curve3d.diamondIcon;
        var curveEditor = new CurveComposite(curve3d);//prob shouldn't do this every frame
        switch (Event.current.type)
        {
            case EventType.Repaint:
                Draw(curveEditor);
                //
                GL.PushMatrix();
                curve3d.testmat.SetPass(0);
                GL.LoadOrtho();
                GL.Begin(GL.TRIANGLES);
                GL.Vertex3(0, 0, -10f);
                GL.Vertex3(1, 1, -10f);
                GL.Vertex3(0, 1, -10f);
                GL.End();
                GL.PopMatrix();
                break;
        }
    }
    void Draw(IComposite drawTarget)
    {
        List<IDraw> draws = new List<IDraw>();
        drawTarget.Draw(draws);
        draws.Sort((a, b) => Mathf.CeilToInt(Mathf.Sign(b.DistFromCamera() - a.DistFromCamera())));
        Handles.BeginGUI();
        foreach (var draw in draws)
            if (draw.DistFromCamera()>0)
                draw.Draw();
        Handles.EndGUI();
    }
}
