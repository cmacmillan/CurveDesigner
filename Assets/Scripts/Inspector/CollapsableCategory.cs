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

        private const int kSingleLineHeight = 18;

        protected static Rect GetControlRect(int height,Curve3D curve, params GUILayoutOption[] layoutOptions)
        {
            return GUILayoutUtility.GetRect(0, height, curve.controlRectStyle, layoutOptions);
        }

        private void SamplerField(SerializedObject obj, string path, IValueSampler sampler,Curve3D curve, params GUILayoutOption[] layoutOptions)
        {
            Rect rect = GetControlRect(kSingleLineHeight, curve,layoutOptions);
            Rect popupRect = GetPopupRect(rect);
            popupRect.height = kSingleLineHeight;
            rect = SubtractPopupWidth(rect);

            ValueType state = sampler.ValueType;

            switch (state)
            {
                case ValueType.Constant:
                    /*
                        EditorGUI.PropertyField(rect,obj.FindProperty($"{path}.constValue"),new GUIContent(sampler.GetLabel()));
                        break;
                    case ValueType.Keyframes:
                    */
                    GUI.Label(new Rect(rect.position,new Vector2(EditorGUIUtility.labelWidth,rect.height)), sampler.GetLabel(),EditorStyles.label);
                    rect.xMin += EditorGUIUtility.labelWidth;
                    EditMode thisEditMode = sampler.GetEditMode();
                    if (GUI.Toggle(rect,curve.editMode == thisEditMode, EditorGUIUtility.TrTextContent(sampler.GetLabel()), curve.buttonStyle))
                        curve.editMode = thisEditMode;
                    break;
            }

            // PopUp minmaxState menu
            if (EditorGUI.DropdownButton(popupRect, GUIContent.none, FocusType.Passive, curve.dropdownStyle))
            {
                GUIContent[] texts =        {   EditorGUIUtility.TrTextContent("Constant"),
                                                EditorGUIUtility.TrTextContent("Curve") };
                ValueType[] states = {  ValueType.Constant,
                                        ValueType.Keyframes};
                GenericMenu menu = new GenericMenu();
                for (int i = 0; i < texts.Length; ++i)
                {
                    menu.AddItem(texts[i], state == states[i], SelectValueTypeState,sampler);
                }
                menu.DropDown(popupRect);
                Event.current.Use();
            }
        }

        void SelectValueTypeState(object sampler)
        {
            var valueSampler = sampler as IValueSampler;
            if (valueSampler != null)
            {

            }
        }
        ///////////////////////////////



        public override void Draw(Curve3D curve)
        {
            obj= new SerializedObject(curve);
            float width = Screen.width - 18; // -10 is effect_bg padding, -8 is inspector padding
            /*
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal(curve.settings.modeSelectorStyle);
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
            */

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
                    valueSampler.ValueType = (ValueType)EditorGUILayout.EnumPopup("Value Along Curve",valueSampler.ValueType);
                    if (valueSampler.ValueType == ValueType.Constant)
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
            if (curve.type!= CurveType.NoMesh)
            {
                SamplerField(obj, "sizeSampler", curve.sizeSampler,curve);
                SamplerField(obj, "rotationSampler", curve.rotationSampler,curve);
                if (curve.type == CurveType.Cylinder || curve.type == CurveType.HollowTube)
                {
                    SamplerField(obj, "arcOfTubeSampler", curve.arcOfTubeSampler,curve);
                }
                if (curve.type == CurveType.Mesh)
                {
                    Field("meshToTile");
                    Field("clampAndStretchMeshToCurve");
                }
                if (curve.type != CurveType.Mesh)
                {
                    SamplerField(obj, "thicknessSampler",curve.thicknessSampler,curve);
                }
                if (curve.type != CurveType.Mesh)
                {
                    Field("vertexDensity");
                }
                if (curve.type== CurveType.Cylinder || curve.type== CurveType.DoubleBezier || curve.type == CurveType.HollowTube || curve.type== CurveType.Cylinder)
                {
                    Field("ringPointCount");
                }
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
