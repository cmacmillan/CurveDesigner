using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.NewUI
{
    public class PointDraw : IDraw
    {
        Vector2 _guiPos;
        float _distFromCamera;
        int _size;
        PointTextureType type;
        Color _color;
        IComposite _creator;
        bool hideIfNotHovered;
        
        public PointDraw(IComposite creator,Vector3 position,PointTextureType type,Color color,int size = 5,bool hideIfNotHovered=false)
        {
            GUITools.WorldToGUISpace(position, out _guiPos, out _distFromCamera);
            this._color = color;
            this._size = size;
            this.type = type;
            this._creator = creator;
            this.hideIfNotHovered = hideIfNotHovered;
        }
        public float DistFromCamera()
        {
            return _distFromCamera+(int)IDrawSortLayers.Points;
        }

        public void Draw(DrawMode mode,SelectionState selectionState)
        {
            if (mode == DrawMode.hovered || !hideIfNotHovered)
            {
                var rect = GUITools.GetRectCenteredAtPosition(_guiPos, _size, _size);
                DrawPoint(rect, mode.Tint(selectionState, _color), GetPointTexture(type));
            }
        }
        private static void DrawPoint(Rect position, Color color, Texture2D tex)
        {
            Handles.BeginGUI();
            Color oldColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(position, tex);
            GUI.color = oldColor;
            Handles.EndGUI();
        }
        public static Texture2D GetPointTexture(PointTextureType type)
        {
            switch (type)
            {
                case PointTextureType.circle:
                    return Curve3DSettings.circleTexture;
                case PointTextureType.square:
                    return Curve3DSettings.squareTexture;
                case PointTextureType.diamond:
                    return Curve3DSettings.diamondTexture;
                default:
                    throw new KeyNotFoundException();
            }
        }

        public IComposite Creator()
        {
            return _creator;
        }
    }
}
