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

        protected SerializedObject serializedObj;
        public void Field(string fieldName)
        {
            EditorGUILayout.PropertyField(serializedObj.FindProperty(fieldName));
        }
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

        protected static Rect GetControlRect(int height, Curve3D curve, params GUILayoutOption[] layoutOptions)
        {
            return GUILayoutUtility.GetRect(0, height, curve.controlRectStyle, layoutOptions);
        }

        public void EditModeSwitchButton(string label, EditMode mode, Rect rect,Curve3D curve)
        {
            EditMode thisEditMode = mode;
            bool isSelected = curve.editMode == thisEditMode;
            GUI.Label(new Rect(rect.position, new Vector2(EditorGUIUtility.labelWidth, rect.height)), label, EditorStyles.label);
            rect.xMin += EditorGUIUtility.labelWidth;
            if (GUI.Toggle(rect, isSelected, EditorGUIUtility.TrTextContent($"{(isSelected ? "Editing" : "Edit")} {label}"), curve.buttonStyle))
                curve.editMode = thisEditMode;
        }
        public Rect GetFieldRects(out Rect popupRect,Curve3D curve)
        {
            Rect rect = GetControlRect(kSingleLineHeight, curve);
            popupRect = GetPopupRect(rect);
            popupRect.height = kSingleLineHeight;
            rect = SubtractPopupWidth(rect);
            return rect;
        }
        public void SamplerField(string path, IValueSampler sampler,Curve3D curve)
        {
            Rect rect = GetFieldRects(out Rect popupRect,curve);

            ValueType state = sampler.ValueType;

            switch (state)
            {
                case ValueType.Constant:
                    EditorGUI.PropertyField(rect,serializedObj.FindProperty($"{path}.constValue"), new GUIContent(sampler.GetLabel()));
                    break;
                case ValueType.Keyframes:
                    EditModeSwitchButton(sampler.GetLabel(), sampler.GetEditMode(), rect,curve);
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
                    menu.AddItem(texts[i], state == states[i], SelectValueTypeState, new SelectValueTypeStateTuple(sampler, states[i], curve));
                }
                menu.DropDown(popupRect);
                Event.current.Use();
            }
        }

        private class SelectValueTypeStateTuple
        {
            public IValueSampler sampler;
            public ValueType mode;
            public Curve3D curve;
            public SelectValueTypeStateTuple(IValueSampler sampler, ValueType mode, Curve3D curve)
            {
                this.sampler = sampler;
                this.mode = mode;
                this.curve = curve;
            }
        }
        void SelectValueTypeState(object arg)
        {
            var tuple = arg as SelectValueTypeStateTuple;
            if (tuple != null)
            {
                tuple.sampler.ValueType = tuple.mode;
                if (tuple.mode == ValueType.Constant && tuple.curve.editMode == tuple.sampler.GetEditMode())
                {
                    tuple.curve.editMode = EditMode.PositionCurve;//default to position
                }
                if (tuple.mode == ValueType.Keyframes)
                    tuple.curve.editMode = tuple.sampler.GetEditMode();
                HandleUtility.Repaint();
            }
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


        public override void Draw(Curve3D curve)
        {
            serializedObj = new SerializedObject(curve);
            float width = Screen.width - 18; // -10 is effect_bg padding, -8 is inspector padding
            bool needsReinitCurve = false;
            EditModeSwitchButton("Position", EditMode.PositionCurve,GetFieldRects(out _,curve),curve);
            if (curve.type!= CurveType.NoMesh)
            {
                SamplerField("sizeSampler", curve.sizeSampler,curve);
                SamplerField("rotationSampler", curve.rotationSampler,curve);
                if (curve.type == CurveType.Cylinder || curve.type == CurveType.HollowTube)
                {
                    SamplerField("arcOfTubeSampler", curve.arcOfTubeSampler,curve);
                }
                if (curve.type != CurveType.Mesh)
                {
                    SamplerField("thicknessSampler",curve.thicknessSampler,curve);
                }
            }
            if (curve.type == CurveType.DoubleBezier)
                EditModeSwitchButton("Double Bezier", EditMode.DoubleBezier, GetFieldRects(out _,curve),curve);
            if (curve.type != CurveType.Mesh && curve.type!=CurveType.NoMesh)
            {
                Field("vertexDensity");
            }
            Field("type");
            /*
            if (curve.editMode == EditMode.DoubleBezier && typeBefore == CurveType.DoubleBezier && typeAfter != CurveType.DoubleBezier)
                curve.editMode = EditMode.PositionCurve;
            */
            EditorGUI.BeginChangeCheck();
            Field("isClosedLoop");
            if (EditorGUI.EndChangeCheck())
            {
                needsReinitCurve = true;
            }
            if (curve.type == CurveType.Mesh)
            {
                Field("meshToTile");
                Field("clampAndStretchMeshToCurve");
                Field("meshPrimaryAxis");
                Field("closeTilableMeshGap");
            }
            if (curve.type == CurveType.Cylinder || curve.type == CurveType.DoubleBezier || curve.type == CurveType.HollowTube || curve.type == CurveType.Cylinder)
            {
                Field("ringPointCount");
            }
            serializedObj.ApplyModifiedProperties();
            if (needsReinitCurve)
                curve.UICurve.Initialize();
        }
    }
    public class TexturesCollapsableCategory : CollapsableCategory
    {
        public override string GetName(Curve3D curve) { return "Textures"; }

        public override void Draw(Curve3D curve)
        {
            serializedObj = new SerializedObject(curve);
            //GUILayout.Label("textures");
            Field("seperateInnerOuterTextures");
            serializedObj.ApplyModifiedProperties();
        }
    }
    public class PreferencesCollapsableCategory : CollapsableCategory
    {
        public override string GetName(Curve3D curve) { return "Preferences"; }

        public override void Draw(Curve3D curve)
        {
            serializedObj = new SerializedObject(curve);
            Field("showPointSelectionWindow");
            Field("showPositionHandles");
            Field("showNormals");
            Field("showTangents");
            Field("lockToPositionZero");
            Field("placeLockedPoints");
            Field("settings");
            serializedObj.ApplyModifiedProperties();
        }
    }
}
