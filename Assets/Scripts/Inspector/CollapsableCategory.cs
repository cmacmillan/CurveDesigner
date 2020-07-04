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
        protected SerializedObject obj;
        protected void Field(string name)
        {
            EditorGUILayout.PropertyField(obj.FindProperty(name));
        }
    }
    public class MainCollapsableCategory : CollapsableCategory
    {
        public Dictionary<EditMode, string> editmodeNameMap = new Dictionary<EditMode, string>()
        {
            {EditMode.PositionCurve, "Position"},
            {EditMode.Size, "Size"},
            {EditMode.Rotation, "Rotation"},
            {EditMode.DoubleBezier, "Double Bezier"},
        };
        public EditMode[] editModes;
        public GUIStyle _centeredStyle;
        private GUIStyle CenteredStyle { get
            {
                if (_centeredStyle == null) {
                    _centeredStyle = GUI.skin.GetStyle("Label");
                    _centeredStyle.alignment = TextAnchor.UpperCenter;
                }
                return _centeredStyle;
            }
        }
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
            obj= new SerializedObject(curve);
            //if (GUILayout.Button(isPlaying ? s_Texts.pause : playText, "ButtonLeft"))
            float width = Screen.width - 18; // -10 is effect_bg padding, -8 is inspector padding
            //Field("tabStyle");
            //GUILayout.BeginHorizontal(GUILayout.Width(width-50));
            //if (curve.tab)
            //GUILayout.Button("asdf", curve.tabStyle.style);
            //curve.tabStyle.asdf = "ButtonRight";
            GUILayout.BeginHorizontal();
            //GUILayout.Label("asdf");
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal(curve.tabStyle.style);
            //GUILayout.BeginHorizontal(curve.tabStyle.style2);
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
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(curve.tabStyle.style2);
            GUILayout.Label($"Click on the curve in the scene view to place a {editmodeNameMap[curve.editMode].ToLower()} control point",CenteredStyle);
            if (curve.editMode == EditMode.Size)
                Field("size");
            if (curve.editMode == EditMode.Rotation)
                Field("rotation");
            GUILayout.EndVertical();

            Field("type");
            Field("isClosedLoop");
            if (curve.type!= CurveType.NoMesh)
            {
                if (curve.type == CurveType.Cylinder || curve.type == CurveType.HollowTube)
                    Field("arcOfTube");
                if (curve.type != CurveType.Mesh)
                    Field("thickness");
                if (curve.type != CurveType.Mesh)
                    Field("vertexDensity");
            }
            obj.ApplyModifiedProperties();
        }
    }
    public class TexturesCollapsableCategory : CollapsableCategory
    {
        public override string GetName(Curve3D curve) { return "Textures"; }

        public override void Draw(Curve3D curve)
        {
            obj = new SerializedObject(curve);
            //GUILayout.Label("textures");
            Field("meshPrimaryAxis");
            Field("useSeperateInnerAndOuterFaceTextures");
            Field("meshToTile");
            Field("closeTilableMeshGap");
            obj.ApplyModifiedProperties();
        }
    }
    public class PreferencesCollapsableCategory : CollapsableCategory
    {
        public override string GetName(Curve3D curve) { return "Preferences"; }

        public override void Draw(Curve3D curve)
        {
            obj = new SerializedObject(curve);
            Field("showNormals");
            Field("lockToPositionZero");
            Field("placeLockedPoints");
            Field("settings");
            obj.ApplyModifiedProperties();
        }
    }
}
