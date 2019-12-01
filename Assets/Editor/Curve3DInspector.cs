using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Curve3D))]
public class Curve3DInspector : Editor
{
    private void OnSceneGUI()
    {
        var curve = target as Curve3D;
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
            curve.curve.AddDefaultSegment();
        }
        if (GUILayout.Button("Lock"))
        {
        }
        if (GUILayout.Button("Clear"))
        {
            curve.curve.Initialize();
        }
        GUILayout.EndHorizontal();
        GUILayout.EndArea();

        Handles.EndGUI();

    }
}
