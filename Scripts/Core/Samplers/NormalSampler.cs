using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    [System.Serializable]
    public class NormalSamplerPoint : SamplerPoint<Vector3, NormalSamplerPoint> { }
    [System.Serializable]
    //these vector3s should always be normalized
    public class NormalSampler : ValueSampler<Vector3, NormalSamplerPoint>
    {
        public NormalSampler(string label, Curve3DEditMode editMode) : base(label, editMode) { }

        public NormalSampler(NormalSampler objToClone, bool createNewGuids, Curve3D curve) : base(objToClone, createNewGuids, curve) { }

        protected override Vector3 GetInterpolatedValueAtDistance(float distance, BezierCurve curve)
        {
            if (GetPoints(curve).Count == 0)
                return Vector3.up;//we always need to produce a normalized vector
            return GetValueAtDistance(distance,curve);
        }

#if UNITY_EDITOR
        public override void ConstantField(Rect rect)
        {
            GUI.Label(EditorGUI.PrefixLabel(rect, new GUIContent(GetLabel())),"Automatic Normals");
        }
        public override void SelectEdit(Curve3D curve, List<NormalSamplerPoint> selectedPoints,NormalSamplerPoint mainPoint)
        {
            Vector3 originalValue = mainPoint.value;
            EditorGUIUtility.SetWantsMouseJumping(1);
            Vector3 fieldVal=EditorGUILayout.Vector3Field(GetLabel(), originalValue);
            base.SelectEdit(curve, selectedPoints, mainPoint);
            if (fieldVal == originalValue)
                return;
            //so now we are gonna try to maintain the ratio of the two existing values, while setting the modified value to the target
            //q^2+(q*z/y)^2 = 1-x^2
            //q^2(z/y)^2 = 1-x^2
            //q^2 = (1-x^2)/(z/y)^2;
            //q = sqrt((1-x^2)/(z/y)^2);
            Vector3 valueToWrite;
            if (fieldVal.x != originalValue.x && fieldVal.y == originalValue.y && fieldVal.z == originalValue.z)
            {
                float x = Mathf.Clamp(fieldVal.x, -1, 1);
                float remaining = Mathf.Sqrt(1 - x * x) * Mathf.Sign(originalValue.z);
                if (originalValue.y == 0.0f && originalValue.z == 0.0f)
                    valueToWrite = new Vector3(Mathf.Sign(x), 0,0);
                else if (originalValue.y == 0.0f)
                    valueToWrite = new Vector3(x, 0,remaining);
                else if (originalValue.z == 0.0f)
                    valueToWrite = new Vector3(x, remaining,0);
                else
                {
                    float ratio = originalValue.z / originalValue.y;
                    float q = Mathf.Sqrt((1 - x * x) / (ratio * ratio));
                    float y = q * Mathf.Sign(originalValue.y);
                    float z = q * Mathf.Sign(originalValue.z) * ratio;
                    valueToWrite = new Vector3(x, y, z);
                }
            }
            else if (fieldVal.x == originalValue.x && fieldVal.y != originalValue.y && fieldVal.z == originalValue.z)
            {
                float y = Mathf.Clamp(fieldVal.y, -1, 1);
                float remaining = Mathf.Sqrt(1 - y * y) * Mathf.Sign(originalValue.z);
                if (originalValue.x == 0.0f && originalValue.z == 0.0f)
                    valueToWrite = new Vector3(0, Mathf.Sign(y), 0);
                else if (originalValue.x == 0.0f)
                    valueToWrite = new Vector3(0, y, remaining);
                else if (originalValue.z == 0.0f)
                    valueToWrite = new Vector3(remaining, y,0);
                else
                {
                    float ratio = originalValue.z / originalValue.x;
                    float q = Mathf.Sqrt((1 - y * y) / (ratio * ratio));
                    float x = q * Mathf.Sign(originalValue.x);
                    float z = q * Mathf.Sign(originalValue.z) * ratio;
                    valueToWrite = new Vector3(x, y, z);
                }
            }
            else if (fieldVal.x == originalValue.x && fieldVal.y == originalValue.y && fieldVal.z != originalValue.z)
            {
                float z = Mathf.Clamp(fieldVal.z, -1, 1);
                float remaining = Mathf.Sqrt(1 - z * z) * Mathf.Sign(originalValue.y);
                if (originalValue.x == 0.0f && originalValue.y == 0.0f)
                    valueToWrite = new Vector3(0, 0,Mathf.Sign(z));
                else if (originalValue.x == 0.0f)
                    valueToWrite = new Vector3(0, remaining, z);
                else if (originalValue.y == 0.0f)
                    valueToWrite = new Vector3(remaining, 0, z);
                else
                {
                    float ratio = originalValue.y / originalValue.x;
                    float q = Mathf.Sqrt((1 - z * z) / (ratio * ratio));
                    float x = q * Mathf.Sign(originalValue.x);
                    float y = q * Mathf.Sign(originalValue.y) * ratio;
                    valueToWrite = new Vector3(x, y, z);
                }
            }
            else
            {
                valueToWrite = fieldVal;
            }
            valueToWrite = valueToWrite.normalized;
            foreach (var target in selectedPoints)
                target.Value = valueToWrite;
        }
#endif

        public override Vector3 Lerp(Vector3 val1, Vector3 val2, float lerp)
        {
            return Vector3.Slerp(val1, val2, lerp);
        }
    }
}
