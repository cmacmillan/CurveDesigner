using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{ 
    public interface IDraw
    {
        void Draw(DrawMode mode,SelectionState selectionState);
        float DistFromCamera();
        Composite Creator();
    } 
    public static class DrawModeExtensionMethods
    {
        private static readonly Color primarySelectedColor = new Color(1,.9f,.32f);//yellowish
        private static readonly Color secondarySelectedColor = new Color(1,.78f,.32f);//orangeish
        public static Color Tint(this DrawMode mode,SelectionState selectionState,Color c)
        {
            Color selectionColor;
            switch (selectionState)
            {
                case SelectionState.primarySelected:
                    selectionColor = primarySelectedColor;
                    break;
                case SelectionState.secondarySelected:
                    selectionColor = secondarySelectedColor;
                    break;
                default:
                    selectionColor = c;
                    break;
            }
            switch (mode)
            {
                case DrawMode.clicked:
                    return DarkenColor(selectionColor,.2f);
                case DrawMode.hovered:
                    return DesaturateColor(selectionColor,.8f);
                case DrawMode.normal:
                default:
                    return selectionColor;
            }
        }
        private static Color DesaturateColor(Color color, float amount)
        {
            return Color.Lerp(color, Color.white, amount);
        }
        private static Color DarkenColor(Color color, float amount)
        {
            var retr = color * amount;
            retr.a = color.a;
            return retr;
        }
    }
}
