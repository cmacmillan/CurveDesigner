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
    public class FloatDistanceSampler : ValueDistanceSampler<float, FloatSamplerPoint,FloatDistanceSampler>
    {
        public override float GetDefaultVal() { return 0; }

        public override float Lerp(float val1, float val2, float lerp) { return Mathf.Lerp(val1,val2,lerp); }
    }

    [System.Serializable]
    public class FloatSamplerPoint : FieldEditableSamplerPoint<float,FloatSamplerPoint,FloatDistanceSampler> 
    {
        public override float Field(string displayName, float originalValue)
        {
            return EditorGUILayout.FloatField(displayName, originalValue);
        }

        public override float Add(float v1, float v2) { return v1 + v2; }

        public override float Subtract(float v1, float v2) { return v1 - v2; }

        public override float Zero() { return 0; }
    }
}
