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
        public override string GetName(Curve3D curve) { return curve.name; }

        public override void Draw(Curve3D curve)
        {
            var obj = new SerializedObject(curve);
            EditorGUILayout.PropertyField(obj.FindProperty("type"));
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
            EditorGUILayout.PropertyField(obj.FindProperty("vertexDensity"));
            obj.ApplyModifiedProperties();
        }
    }
}
