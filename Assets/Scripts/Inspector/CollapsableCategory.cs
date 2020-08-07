using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Assets.NewUI
{
    //Abstract classes can't be serialized, but we could make this serializable by adding a concrete root class, saving that, and casting back to the abstract class when we wanna serialize it
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
            {EditMode.Color, "Color"},
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

        /// Shuriken field with dropdown triangle
        protected const float k_minMaxToggleWidth = 13;
        protected static Rect GetPopupRect(Rect position)
        {
            position.xMin = position.xMax - k_minMaxToggleWidth;
            return position;
        }
        protected static Rect SubtractPopupWidth(Rect position)
        {
            position.width -= 1 + k_minMaxToggleWidth;
            return position;
        }
        ///////////////////////////////



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
            GUILayout.BeginHorizontal(curve.settings.modeSelectorStyle);
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
                else if (i -skipCount == editModes.Length - 1-skipCount)
                    style = "ButtonRight";
                else
                    style = "ButtonMid";
                if (GUILayout.Toggle(curve.editMode == currMode,EditorGUIUtility.TrTextContent(currName), style))
                    curve.editMode = currMode;
            }
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(curve.settings.selectorWindowStyle);
            bool shouldDrawWindowContent = true;
            switch (curve.editMode)
            {
                case EditMode.PositionCurve:
                    break;
                case EditMode.DoubleBezier:
                    break;
                default:
                    var valueSampler = curve.ActiveElement as IValueSampler;
                    valueSampler.Interpolation = (InterpolationType)EditorGUILayout.EnumPopup("Value Along Curve",valueSampler.Interpolation);
                    if (valueSampler.Interpolation == InterpolationType.Constant)
                    {
                        shouldDrawWindowContent = false;
                        if (curve.editMode == EditMode.Size)
                            curve.sizeSampler.constValue = EditorGUILayout.FloatField("Size",curve.sizeSampler.constValue);
                        if (curve.editMode == EditMode.Rotation)
                            curve.rotationSampler.constValue = EditorGUILayout.FloatField("Rotation",curve.rotationSampler.constValue);
                        if (curve.editMode == EditMode.Color)
                            curve.colorSampler.constValue = EditorGUILayout.ColorField("Color",curve.colorSampler.constValue);
                    }
                    break;
            }
            if (shouldDrawWindowContent && curve.UICurve != null)
            {
                int pointCount = curve.ActiveElement.NumSelectables(curve);
                if (pointCount == 0)
                {
                    GUILayout.Label($"Click on the curve in the scene view to place a {editmodeNameMap[curve.editMode].ToLower()} control point", CenteredStyle);
                }
                else
                {
                    int selectedPointCount = 0;
                    int numPoints = curve.ActiveElement.NumSelectables(curve);
                    for (int i = 0; i < numPoints; i++)
                        if (curve.selectedPoints.Contains(curve.ActiveElement.GetSelectable(i, curve).GUID))
                            selectedPointCount++;
                    if (selectedPointCount == 0)
                        GUILayout.Label("No points selected",CenteredStyle);
                    else
                        GUILayout.Label($"{selectedPointCount} point{(selectedPointCount != 1 ? "s" : "")} selected", CenteredStyle);
                    var drawer = curve.UICurve.GetWindowDrawer();
                    drawer.DrawWindow(curve);
                }
            }
            GUILayout.EndVertical();

            Field("type");
            Field("isClosedLoop");
            /*
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.FloatField("taco",5);
            EditorGUILayout.DropdownButton(GUIContent.none,FocusType.Passive,dropdownStyle);
            EditorGUILayout.EndHorizontal();
            */
            if (curve.type!= CurveType.NoMesh)
            {
                if (curve.type== CurveType.Cylinder || curve.type== CurveType.DoubleBezier || curve.type == CurveType.HollowTube || curve.type== CurveType.Cylinder)
                {
                    Field("ringPointCount");
                }
                if (curve.type == CurveType.Cylinder || curve.type == CurveType.HollowTube)
                {
                    Field("arcOfTube");
                }
                if (curve.type == CurveType.Mesh)
                {
                    Field("meshToTile");
                    Field("clampAndStretchMeshToCurve");
                }
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
            Field("showTangents");
            Field("lockToPositionZero");
            Field("placeLockedPoints");
            Field("settings");
            obj.ApplyModifiedProperties();
        }
    }
}
