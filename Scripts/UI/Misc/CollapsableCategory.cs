#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    //Abstract classes can't be serialized, but we could make this serializable by adding a concrete root class, saving that, and casting back to the abstract class when we wanna serialize it
    public abstract class CollapsableCategory
    {
        public abstract bool IsExpanded(Curve3D curve);
        public abstract void SetIsExpanded(Curve3D curve,bool value);
        public abstract string GetName(Curve3D curve);
        public abstract void Draw(Curve3D curve);

        protected SerializedObject serializedObj;
        public void Field(string fieldName, bool isRed=false)
        {
            var property = serializedObj.FindProperty(fieldName);
            var oldColor = GUI.color;
            if (isRed)
                GUI.color= Color.red;
            EditorGUILayout.PropertyField(property);
            if (isRed)
                GUI.color= oldColor;
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

        public void EditModeSwitchButton(string label, Curve3DEditMode mode, Rect rect,Curve3D curve)
        {
            Curve3DEditMode thisEditMode = mode;
            bool isSelected = curve.editMode == thisEditMode;
            GUI.Label(new Rect(rect.position, new Vector2(EditorGUIUtility.labelWidth, rect.height)), label, EditorStyles.label);
            rect.xMin += EditorGUIUtility.labelWidth;
            if (GUI.Toggle(rect, isSelected, EditorGUIUtility.TrTextContent($"{(isSelected ? "Editing" : "Edit")} {label}"), curve.buttonStyle))
            {
                curve.editMode = thisEditMode;
                SceneView.RepaintAll();
            }
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

            if (sampler.UseKeyframes)
            {
                EditModeSwitchButton(sampler.GetLabel(), sampler.GetEditMode(), rect, curve);
            }
            else
            {
                sampler.ConstantField(rect);
            }

            // PopUp minmaxState menu
            if (EditorGUI.DropdownButton(popupRect, GUIContent.none, FocusType.Passive, curve.dropdownStyle))
            {
                GUIContent[] texts =        {   EditorGUIUtility.TrTextContent("Constant"),
                                                EditorGUIUtility.TrTextContent("Curve") };
                var currentState = sampler.UseKeyframes;
                bool[] states = {  false,
                                    true};
                GenericMenu menu = new GenericMenu();
                for (int i = 0; i < texts.Length; ++i)
                {
                    menu.AddItem(texts[i], currentState== states[i], SelectUseKeyframes, new UseKeyframesDropdownTuple(sampler, states[i], curve));
                }
                menu.DropDown(popupRect);
                Event.current.Use();
            }
        }

        private class UseKeyframesDropdownTuple
        {
            public IValueSampler sampler;
            public bool useKeyframes = false;
            public Curve3D curve;
            public UseKeyframesDropdownTuple(IValueSampler sampler, bool useKeyframes, Curve3D curve)
            {
                this.sampler = sampler;
                this.useKeyframes = useKeyframes;
                this.curve = curve;
            }
        }
        void SelectUseKeyframes(object arg)
        {
            var tuple = arg as UseKeyframesDropdownTuple;
            if (tuple != null)
            {
                tuple.sampler.UseKeyframes = tuple.useKeyframes;
                if (tuple.useKeyframes)
                    tuple.curve.editMode = tuple.sampler.GetEditMode();
                else if (tuple.curve.editMode == tuple.sampler.GetEditMode())
                    tuple.curve.editMode = Curve3DEditMode.PositionCurve;//default to position
                HandleUtility.Repaint();
            }
        }
    }
    public class MainCollapsableCategory : CollapsableCategory
    {
        public Dictionary<Curve3DEditMode, string> editmodeNameMap = new Dictionary<Curve3DEditMode, string>()
        {
            {Curve3DEditMode.PositionCurve, "Position"},
            {Curve3DEditMode.Size, "Size"},
            {Curve3DEditMode.Rotation, "Rotation"},
            {Curve3DEditMode.Extrude, "Extrude"},
            {Curve3DEditMode.Color, "Color"},
        };
        public Curve3DEditMode[] editModes;
        public MainCollapsableCategory()
        {
            var baseEditModes = System.Enum.GetValues(typeof(Curve3DEditMode));
            var baseEditModeNames = System.Enum.GetNames(typeof(Curve3DEditMode));
            editModes = new Curve3DEditMode[baseEditModes.Length];
            for (int i = 0; i < editModes.Length; i++)
                editModes[i] = (Curve3DEditMode)baseEditModes.GetValue(i);
        }

        public override string GetName(Curve3D curve) { return curve.name; }

        public override void Draw(Curve3D curve)
        {
            serializedObj = new SerializedObject(curve);
            float width = Screen.width - 18; // -10 is effect_bg padding, -8 is inspector padding
            bool needsReinitCurve = false;
            bool changedCurveType = false;
            EditorGUI.BeginChangeCheck();
            Field("type");
            if (EditorGUI.EndChangeCheck())
            {
                changedCurveType = true;
            }
            EditorGUI.BeginChangeCheck();
            Field("positionCurve.isClosedLoop");
            Field("positionCurve.automaticTangents");
            if (curve.positionCurve.automaticTangents)
            {
                Field("positionCurve.automaticTangentSmoothing");
            }
            if (EditorGUI.EndChangeCheck())
            {
                needsReinitCurve = true;
            }
            EditModeSwitchButton("Position", Curve3DEditMode.PositionCurve,GetFieldRects(out _,curve),curve);
            if (curve.type!= MeshGenerationMode.NoMesh)
            {
                if (curve.type!= MeshGenerationMode.Extrude)
                {
                    SamplerField("sizeSampler", curve.sizeSampler, curve);
                }
                SamplerField("rotationSampler", curve.rotationSampler,curve);
                if (curve.type == MeshGenerationMode.Cylinder || curve.type == MeshGenerationMode.HollowTube)
                {
                    SamplerField("arcOfTubeSampler", curve.arcOfTubeSampler,curve);
                }
                if (curve.type != MeshGenerationMode.Mesh && curve.type!=MeshGenerationMode.Cylinder)
                {
                    SamplerField("thicknessSampler",curve.thicknessSampler,curve);
                }
                SamplerField("colorSampler", curve.colorSampler, curve);
            }
            if (curve.type == MeshGenerationMode.Extrude)
                EditModeSwitchButton("Extrude", Curve3DEditMode.Extrude, GetFieldRects(out _,curve),curve);
            if (curve.type == MeshGenerationMode.Mesh)
            {
                Field("meshToTile",curve.meshToTile==null);
                Field("clampAndStretchMeshToCurve");
                Field("meshPrimaryAxis");
                Field("closeTilableMeshGap");
            }
            serializedObj.ApplyModifiedProperties();
            if (changedCurveType)
                curve.OnCurveTypeChanged();
            if (needsReinitCurve)
                curve.UICurve.Initialize();
        }

        public override bool IsExpanded(Curve3D curve)
        {
            return curve.mainCategoryExpanded;
        }

        public override void SetIsExpanded(Curve3D curve, bool value)
        {
            curve.mainCategoryExpanded = value;
        }
    }
    public class MeshGenerationCollapsableCategory : CollapsableCategory
    {
        public override string GetName(Curve3D curve) { return "Mesh Generation"; }
        public override void Draw(Curve3D curve)
        {
            if (curve.type == MeshGenerationMode.NoMesh)
                return;
            serializedObj = new SerializedObject(curve);
            bool needsReinitCurve = false;
            if (curve.type != MeshGenerationMode.Mesh && curve.type!=MeshGenerationMode.NoMesh)
            {
                Field("vertexDensity");
            }
            if (curve.type == MeshGenerationMode.Cylinder || curve.type == MeshGenerationMode.HollowTube || curve.type == MeshGenerationMode.Extrude)
            {
                Field("ringPointCount");
            }
            if (curve.type == MeshGenerationMode.Cylinder || curve.type == MeshGenerationMode.Flat)
            {
                Field("flatPointCount");
            }
            Field("lockToPositionZero");
            EditorGUI.BeginChangeCheck();
            Field("normalGenerationMode");
            if (EditorGUI.EndChangeCheck())
                needsReinitCurve = true;
            serializedObj.ApplyModifiedProperties();
            if (needsReinitCurve)
                curve.UICurve.Initialize();
        }

        public override bool IsExpanded(Curve3D curve)
        {
            return curve.meshGenerationCategoryExpanded;
        }

        public override void SetIsExpanded(Curve3D curve, bool value)
        {
            curve.meshGenerationCategoryExpanded = value;
        }
    }
    public class TexturesCollapsableCategory : CollapsableCategory
    {
        public override string GetName(Curve3D curve) { return "Textures"; }

        protected const int kSingleLineHeight = 13;
        private void TextureField(string propName,string name,Curve3D curve,TextureLayer layer)
        {
            GUILayout.Space(2);
            GUILayout.BeginVertical(name, curve.shurikenCustomDataWindow);
            Field($"{propName}.material");
            Field($"{propName}.settings.textureGenMode");
            Field($"{propName}.settings.textureDirection");
            Field($"{propName}.settings.scale");
            GUILayout.EndVertical();
        }
        public override void Draw(Curve3D curve)
        {
            if (curve.type == MeshGenerationMode.NoMesh)
                return;
            serializedObj = new SerializedObject(curve);
            EditorGUI.BeginChangeCheck();
            if (curve.ShouldUseMainTextureLayer())
                TextureField("mainTextureLayer","Front",curve,curve.mainTextureLayer);
            if (curve.ShouldUseBackTextureLayer())
                TextureField("backTextureLayer","Back",curve,curve.backTextureLayer);
            if (curve.ShouldUseEdgeTextureLayer())
                TextureField("edgeTextureLayer","Edge",curve,curve.edgeTextureLayer);
            if (curve.ShouldUseEndTextureLayer())
                TextureField("endTextureLayer","End",curve,curve.endTextureLayer);
            serializedObj.ApplyModifiedProperties();
            if (EditorGUI.EndChangeCheck())
            {
                curve.WriteMaterialsToRenderer();
            }
        }

        public override bool IsExpanded(Curve3D curve)
        {
            return curve.textureCategoryExpanded;
        }

        public override void SetIsExpanded(Curve3D curve, bool value)
        {
            curve.textureCategoryExpanded = value;
        }
    }
    public class AdvancedCollapsableCategory : CollapsableCategory
    {
        public override string GetName(Curve3D curve) { return "Advanced"; }
        public override void Draw(Curve3D curve)
        {
            EditorGUILayout.HelpBox("SamplesPerSegment increases the accuracy with which the curve is approximated. Increasing this may improve curve quality by reducing vertex and texture distortion, but will reduce performance.", MessageType.Info);
            serializedObj = new SerializedObject(curve);
            bool needsReinitCurve=false;
            EditorGUI.BeginChangeCheck();
            Field("samplesPerSegment");
            if (EditorGUI.EndChangeCheck())
            {
                needsReinitCurve = true;
            }
            serializedObj.ApplyModifiedProperties();
            if (needsReinitCurve)
                curve.UICurve.Initialize();
        }

        public override bool IsExpanded(Curve3D curve)
        {
            return curve.advancedCategoryExpanded;
        }

        public override void SetIsExpanded(Curve3D curve, bool value)
        {
            curve.advancedCategoryExpanded= value;
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
            Field("placeLockedPoints");
            serializedObj.ApplyModifiedProperties();
        }

        public override bool IsExpanded(Curve3D curve)
        {
            return curve.preferencesCategoryExpanded;
        }

        public override void SetIsExpanded(Curve3D curve, bool value)
        {
            curve.preferencesCategoryExpanded = value;
        }
    }
}
#endif
