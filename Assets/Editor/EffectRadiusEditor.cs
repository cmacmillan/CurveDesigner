using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(EffectRadius))]
public class EffectRadiusEditor : Editor
{
    public void OnSceneGUI()
    {
        EffectRadius t = (target as EffectRadius);

        foreach (var i in t.areaOfEffects)
        {
            EditorGUI.BeginChangeCheck();
            //float areaOfEffect = Handles.RadiusHandle(Quaternion.identity, t.transform.position, i.AoE);
            Vector3 areaOfEffect = Handles.PositionHandle(i.AoE,Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Changed Area Of Effect");
                i.AoE = areaOfEffect;
            }
        }
    }
}
