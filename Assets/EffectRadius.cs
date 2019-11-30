using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EffectRadius : MonoBehaviour
{
    public List<AreaOfEffectItem> areaOfEffects;
}
[System.Serializable]
public class AreaOfEffectItem
{
    public Vector3 AoE;
}
