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
    }
}
