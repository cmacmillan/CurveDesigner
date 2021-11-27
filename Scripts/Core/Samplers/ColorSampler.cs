using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    [System.Serializable]
    public class ColorSamplerPoint : SamplerPoint<Color,ColorSamplerPoint> { }

    [System.Serializable]
    public class ColorSampler : ValueSampler<Color,ColorSamplerPoint>
    {
        public ColorSampler(string label, Curve3DEditMode editMode) : base(label, editMode) {
            constValue = Color.white;
        }
        public ColorSampler(ColorSampler objToClone,bool createNewGuids,Curve3D curve) : base(objToClone,createNewGuids,curve) { }
#if UNITY_EDITOR
        public override void ConstantField(Rect rect)
        {
            constValue = EditorGUI.ColorField(rect, GetLabel(), constValue);
        }
        public override void SelectEdit(Curve3D curve, List<ColorSamplerPoint> selectedPoints, ColorSamplerPoint mainPoint)
        {
            if (selectedPoints.Count==1)
            {
                var originalValue = mainPoint.value;
                var label = new GUIContent();
                label.text = GetLabel();
                var newColor = EditorGUILayout.ColorField(label, originalValue, showEyedropper: false, showAlpha: true, hdr: false);
                selectedPoints[0].value = newColor;
            }
            base.SelectEdit(curve, selectedPoints, mainPoint);
        }
#endif

        public override Color Lerp(Color val1, Color val2, float lerp)
        {
            //this looks better when unity is set to linear rather than gamma color space
            //see https://docs.unity3d.com/Manual/LinearRendering-LinearOrGammaWorkflow.html
            //and https://www.youtube.com/watch?v=LKnqECcg6Gw
            //if working in gamma, you can uncomment the following line to improve the appearance
            //return Color.Lerp(val1.linear,val2.linear,lerp).gamma
            return Color.Lerp(val1, val2,lerp);
        }
    }
}
