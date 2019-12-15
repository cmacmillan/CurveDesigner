using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Curve3D))]
public class Curve3DInspector : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }
    private void OnSceneGUI()
    {
        var curve = target as Curve3D;
        Undo.RecordObject(curve, "curve");
        MyGUI.EditBezierCurve(curve, curve.transform.position);
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
            curve.curveSize = new List<FloatFieldKeyframe>();
            curve.curveSize.Add(new FloatFieldKeyframe(0,1.0f));
        }
        GUILayout.EndHorizontal();
        GUILayout.EndArea();

        Handles.EndGUI();

    }
}
