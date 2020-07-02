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
        Dictionary<EditMode, string> editmodeNameMap = new Dictionary<EditMode, string>()
        {
            {EditMode.PositionCurve, "Position"},
            {EditMode.Size, "Size"},
            {EditMode.Rotation, "Rotation"},
            {EditMode.DoubleBezier, "Double Bezier"},
        };
        EditMode[] editModes;
        public MainCollapsableCategory()
        {
            isExpanded = true;
            var baseEditModes = System.Enum.GetValues(typeof(EditMode));
            var baseEditModeNames = System.Enum.GetNames(typeof(EditMode));
            editModes = new EditMode[baseEditModes.Length];
            for (int i = 0; i < editModes.Length; i++)
                editModes[i] = (EditMode)baseEditModes.GetValue(i);
        }

        public override string GetName(Curve3D curve) { return curve.name; }

        public override void Draw(Curve3D curve)
        {
            //if (GUILayout.Button(isPlaying ? s_Texts.pause : playText, "ButtonLeft"))
            float width = Screen.width - 18; // -10 is effect_bg padding, -8 is inspector padding
            //GUILayout.BeginHorizontal(GUILayout.Width(width-50));
            GUILayout.BeginHorizontal();
            GUILayout.Label("asdf");
            GUILayout.FlexibleSpace();
            int skipCount = 0;
            if (curve.type != CurveType.DoubleBezier)
                skipCount++;
            for (int i = 0; i < editModes.Length; i++)
            {
                EditMode currMode = editModes[i];
                if (curve.type != CurveType.DoubleBezier && currMode == EditMode.DoubleBezier)
                    continue;
                string currName = editmodeNameMap[currMode];
                string style;
                if (i == 0)
                    style = "ButtonLeft";
                else if (i == editModes.Length - 1-skipCount)
                    style = "ButtonRight";
                else
                    style = "ButtonMid";
                if (GUILayout.Toggle(curve.editMode == currMode,EditorGUIUtility.TrTextContent(currName), style))
                    curve.editMode = currMode;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            var obj = new SerializedObject(curve);
            EditorGUILayout.PropertyField(obj.FindProperty("type"));
            EditorGUILayout.PropertyField(obj.FindProperty("isClosedLoop"));
            if (curve.type!= CurveType.NoMesh)
            {
                if (curve.type == CurveType.Cylinder || curve.type == CurveType.HollowTube)
                    EditorGUILayout.PropertyField(obj.FindProperty("arcOfTube"));
                EditorGUILayout.PropertyField(obj.FindProperty("size"));
                EditorGUILayout.PropertyField(obj.FindProperty("rotation"));
                if (curve.type != CurveType.Mesh)
                    EditorGUILayout.PropertyField(obj.FindProperty("thickness"));
                if (curve.type != CurveType.Mesh)
                    EditorGUILayout.PropertyField(obj.FindProperty("vertexDensity"));
            }
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
