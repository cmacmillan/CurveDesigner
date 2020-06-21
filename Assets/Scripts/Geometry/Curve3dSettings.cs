using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "Curve3DSettings", order = 1)]
public class Curve3dSettings : ScriptableObject
{
    [Tooltip("How accuratly should the curve perform length calculations? Increase to improve accuracy, decrease to improve speed")]
    public int samplesPerSegment = 30;
    public Texture2D lineTex;
    public Texture2D circleIcon;
    public Texture2D squareIcon;
    public Texture2D diamondIcon;
    public Texture2D plusButton;
    public int textureSize = 2048;
}
