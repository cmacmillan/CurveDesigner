using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Assets.NewUI
{
    //Abstract classes can't be serialized
    public abstract class CollapsableCategory
    {
        public bool isExpanded;
        public abstract string GetName(Curve3D curve);
        public abstract void Draw(Curve3D curve);
    }
    public class MainCollapsableCategory : CollapsableCategory
    {
        public MainCollapsableCategory()
        {
            isExpanded = true;
        }
        public override string GetName(Curve3D curve) { return curve.name; }

        public override void Draw(Curve3D curve)
        {
            var obj = new SerializedObject(curve);
            EditorGUILayout.PropertyField(obj.FindProperty("type"));
            EditorGUILayout.PropertyField(obj.FindProperty("editMode"));
            EditorGUILayout.PropertyField(obj.FindProperty("arcOfTube"));
            EditorGUILayout.PropertyField(obj.FindProperty("size"));
            EditorGUILayout.PropertyField(obj.FindProperty("rotation"));
            EditorGUILayout.PropertyField(obj.FindProperty("thickness"));
            EditorGUILayout.PropertyField(obj.FindProperty("isClosedLoop"));
            EditorGUILayout.PropertyField(obj.FindProperty("vertexDensity"));
            obj.ApplyModifiedProperties();
        }
    }
    public class TexturesCollapsableCategory : CollapsableCategory
    {
        public override string GetName(Curve3D curve) { return "Textures"; }

        public override void Draw(Curve3D curve)
        {
            var obj = new SerializedObject(curve);
            //GUILayout.Label("textures");
            EditorGUILayout.PropertyField(obj.FindProperty("meshPrimaryAxis"));
            EditorGUILayout.PropertyField(obj.FindProperty("useSeperateInnerAndOuterFaceTextures"));
            EditorGUILayout.PropertyField(obj.FindProperty("meshToTile"));
            EditorGUILayout.PropertyField(obj.FindProperty("closeTilableMeshGap"));
            obj.ApplyModifiedProperties();
        }
    }
    public class PreferencesCollapsableCategory : CollapsableCategory
    {
        public override string GetName(Curve3D curve) { return "Preferences"; }

        public override void Draw(Curve3D curve)
        {
            var obj = new SerializedObject(curve);
            EditorGUILayout.PropertyField(obj.FindProperty("showNormals"));
            EditorGUILayout.PropertyField(obj.FindProperty("lockToPositionZero"));
            EditorGUILayout.PropertyField(obj.FindProperty("placeLockedPoints"));
            EditorGUILayout.PropertyField(obj.FindProperty("settings"));
            obj.ApplyModifiedProperties();
        }
    }
}
