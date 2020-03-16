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
    public static class DrawModeExtensionMethods
    {
        public static Color Tint(this DrawMode mode,Color c)
        {
            switch (mode)
            {
                case DrawMode.clicked:
                    return DarkenColor(c,.2f);
                case DrawMode.hovered:
                    return DesaturateColor(c,.8f);
                case DrawMode.normal:
                default:
                    return c;
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
        void Draw(DrawMode mode);
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
