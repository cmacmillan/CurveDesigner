using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public class GUITexturesAndStyles : ScriptableObject
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
        [Range(.1f,3f)]
        public float buttonSizeScalar = 1;
    }
}
