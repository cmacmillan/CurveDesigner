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
            float width = Screen.width - 18; // -10 is effect_bg padding, -8 is inspector padding
            bool needsReinitCurve = false;
            curve.EditModeSwitchButton("Position", EditMode.PositionCurve,curve.GetFieldRects(out _));
            if (curve.type!= CurveType.NoMesh)
            {
                curve.SamplerField("sizeSampler", curve.sizeSampler);
                curve.SamplerField("rotationSampler", curve.rotationSampler);
                if (curve.type == CurveType.Cylinder || curve.type == CurveType.HollowTube)
                {
                    curve.SamplerField("arcOfTubeSampler", curve.arcOfTubeSampler);
                }
                if (curve.type != CurveType.Mesh)
                {
                    curve.SamplerField("thicknessSampler",curve.thicknessSampler);
                }
            }
            if (curve.type == CurveType.DoubleBezier)
                curve.EditModeSwitchButton("Double Bezier", EditMode.DoubleBezier, curve.GetFieldRects(out _));
            if (curve.type != CurveType.Mesh && curve.type!=CurveType.NoMesh)
            {
                curve.Field("vertexDensity");
            }
            curve.Field("type");
            /*
            if (curve.editMode == EditMode.DoubleBezier && typeBefore == CurveType.DoubleBezier && typeAfter != CurveType.DoubleBezier)
                curve.editMode = EditMode.PositionCurve;
            */
            EditorGUI.BeginChangeCheck();
            curve.Field("isClosedLoop");
            if (EditorGUI.EndChangeCheck())
            {
                needsReinitCurve = true;
            }
            if (curve.type == CurveType.Mesh)
            {
                curve.Field("meshToTile");
                curve.Field("clampAndStretchMeshToCurve");
                curve.Field("meshPrimaryAxis");
                curve.Field("closeTilableMeshGap");
            }
            if (curve.type == CurveType.Cylinder || curve.type == CurveType.DoubleBezier || curve.type == CurveType.HollowTube || curve.type == CurveType.Cylinder)
            {
                curve.Field("ringPointCount");
            }
            curve.ApplyFieldChanges();
            if (needsReinitCurve)
                curve.UICurve.Initialize();
        }
    }
    public class TexturesCollapsableCategory : CollapsableCategory
    {
        public override string GetName(Curve3D curve) { return "Textures"; }

        public override void Draw(Curve3D curve)
        {
            //GUILayout.Label("textures");
            curve.Field("seperateInnerOuterTextures");
            curve.ApplyFieldChanges();
        }
    }
    public class PreferencesCollapsableCategory : CollapsableCategory
    {
        public override string GetName(Curve3D curve) { return "Preferences"; }

        public override void Draw(Curve3D curve)
        {
            curve.Field("showPointSelectionWindow");
            curve.Field("showPositionHandles");
            curve.Field("showNormals");
            curve.Field("showTangents");
            curve.Field("lockToPositionZero");
            curve.Field("placeLockedPoints");
            curve.Field("settings");
            curve.ApplyFieldChanges();
        }
    }
}
