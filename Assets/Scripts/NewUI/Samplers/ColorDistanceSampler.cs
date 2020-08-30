using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.NewUI
{
    [System.Serializable]
    public class ColorDistanceSampler : ValueDistanceSampler<Color, ColorSamplerPoint, ColorDistanceSampler>
    {
        public ColorDistanceSampler(string label,EditMode editMode): base(label,editMode) {
            constValue = Color.white;
        }

        public ColorDistanceSampler(ColorDistanceSampler objToClone) : base(objToClone) { }

        protected override Color CloneValue(Color value)
        {
            return value;
        }

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
    [System.Serializable]
    public class ColorSamplerPoint : FieldEditableSamplerPoint<Color, ColorSamplerPoint, ColorDistanceSampler>
    {
        public override Color Field(string displayName, Color originalValue)
        {
            var label = new GUIContent();
            label.text = displayName;
            return EditorGUILayout.ColorField(label, originalValue,showEyedropper:false,showAlpha:true,hdr:false);
        }

        public override Color Add(Color v1, Color v2) { return v1 + v2; }

        public override Color Subtract(Color v1, Color v2) { return v1 - v2; }

        public override Color Zero() { return Color.black; }

        public override Color CloneValue(Color value)
        {
            return value;
        }
    }
}
