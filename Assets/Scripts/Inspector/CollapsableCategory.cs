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

        private void EditModeSwitchButton(string label, EditMode mode,Curve3D curve, Rect rect)
        {
            EditMode thisEditMode = mode;
            bool isSelected = curve.editMode == thisEditMode;
            GUI.Label(new Rect(rect.position, new Vector2(EditorGUIUtility.labelWidth, rect.height)), label, EditorStyles.label);
            rect.xMin += EditorGUIUtility.labelWidth;
            if (GUI.Toggle(rect, isSelected, EditorGUIUtility.TrTextContent($"{(isSelected ? "Editing" : "Edit")} {label}"), curve.buttonStyle))
                curve.editMode = thisEditMode;
        }
        private Rect GetFieldRects(Curve3D curve,out Rect popupRect)
        {
            Rect rect = GetControlRect(kSingleLineHeight, curve);
            popupRect = GetPopupRect(rect);
            popupRect.height = kSingleLineHeight;
            rect = SubtractPopupWidth(rect);
            return rect;
        }
        private void SamplerField(SerializedObject obj, string path, IValueSampler sampler, Curve3D curve)
        {
            Rect rect = GetFieldRects(curve,out Rect popupRect);

            ValueType state = sampler.ValueType;

            switch (state)
            {
                case ValueType.Constant:
                    EditorGUI.PropertyField(rect, obj.FindProperty($"{path}.constValue"), new GUIContent(sampler.GetLabel()));
                    break;
                case ValueType.Keyframes:
                    EditModeSwitchButton(sampler.GetLabel(),sampler.GetEditMode(),curve,rect);
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
                    menu.AddItem(texts[i], state == states[i], SelectValueTypeState, new SelectValueTypeStateTuple(sampler,states[i]));
                }
                menu.DropDown(popupRect);
                Event.current.Use();
            }
        }

        private class SelectValueTypeStateTuple
        {
            public IValueSampler sampler;
            public ValueType mode;
            public SelectValueTypeStateTuple(IValueSampler sampler, ValueType mode)
            {
                this.sampler = sampler;
                this.mode = mode;
            }
        }
        void SelectValueTypeState(object arg)
        {
            var tuple = arg as SelectValueTypeStateTuple;
            if (tuple != null)
            {
                tuple.sampler.ValueType = tuple.mode;
            }
        }
        ///////////////////////////////



        public override void Draw(Curve3D curve)
        {
            obj= new SerializedObject(curve);
            float width = Screen.width - 18; // -10 is effect_bg padding, -8 is inspector padding
            /*
            bool shouldDrawWindowContent = true;
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
            */
            EditModeSwitchButton("Position", EditMode.PositionCurve,curve,GetFieldRects(curve,out _));
            if (curve.type!= CurveType.NoMesh)
            {
                SamplerField(obj, "sizeSampler", curve.sizeSampler,curve);
                SamplerField(obj, "rotationSampler", curve.rotationSampler,curve);
                if (curve.type == CurveType.Cylinder || curve.type == CurveType.HollowTube)
                {
                    SamplerField(obj, "arcOfTubeSampler", curve.arcOfTubeSampler,curve);
                }
                if (curve.type != CurveType.Mesh)
                {
                    SamplerField(obj, "thicknessSampler",curve.thicknessSampler,curve);
                }
            }
            if (curve.type == CurveType.DoubleBezier)
                EditModeSwitchButton("Double Bezier", EditMode.DoubleBezier, curve,GetFieldRects(curve,out _));
            if (curve.type != CurveType.Mesh && curve.type!=CurveType.NoMesh)
            {
                Field("vertexDensity");
            }
            Field("type");
            Field("isClosedLoop");
            if (curve.type == CurveType.Mesh)
            {
                Field("meshToTile");
                Field("clampAndStretchMeshToCurve");
            }
            if (curve.type == CurveType.Cylinder || curve.type == CurveType.DoubleBezier || curve.type == CurveType.HollowTube || curve.type == CurveType.Cylinder)
            {
                Field("ringPointCount");
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
