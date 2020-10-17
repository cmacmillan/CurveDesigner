using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "Curve3DSettings", order = 1)]
public class Curve3dSettings : ScriptableObject
{
    public Texture2D lineTex;
    public Texture2D circleIcon;
    public Texture2D squareIcon;
    public Texture2D diamondIcon;
    public Texture2D plusButton;
    public Texture2D uiIcon;
    public Texture2D uiArea;
    public GUIStyle modeSelectorStyle;
    public GUIStyle selectorWindowStyle;
    public GUIStyle colorPickerBoxStyle;
}
