using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public enum DrawMode
    {
        unknown=0,
        normal=1,
        hovered=2,
        clicked=3,
    }
    public enum SelectionState
    {
        unselected=0,
        primarySelected=1,
        secondarySelected=2,
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
    public enum PointTextureType {
        circle = 0,
        square = 1,
        diamond = 2,
    }
    public interface IDraw
    {
        void Draw(DrawMode mode,SelectionState selectionState);
        float DistFromCamera();
        IComposite Creator();
    } 
    public enum IDrawSortLayers
    {
        Points = 0,
        Circles = 500,
        Lines = 1000,
        Curves = 2000,
    }
}
