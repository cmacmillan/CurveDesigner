#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public class PointDraw : IDraw
    {
        Vector2 _guiPos;
        float _distFromCamera;
        int _size;
        PointTextureType type;
        Color _color;
        Composite _creator;
        bool hideIfNotHovered;
        
        public PointDraw(Composite creator,Vector3 position,PointTextureType type,Color color,int size = 5,bool hideIfNotHovered=false)
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
            return _distFromCamera+(int)DrawSortLayers.Points;
        }

        public void Draw(DrawMode mode,SelectionState selectionState)
        {
            if (mode == DrawMode.hovered || !hideIfNotHovered)
            {
                var rect = GUITools.GetRectCenteredAtPosition(_guiPos, _size*CurveUIStatic.buttonSizeScalar, _size*CurveUIStatic.buttonSizeScalar);
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
                    return CurveUIStatic.circleTexture;
                case PointTextureType.square:
                    return CurveUIStatic.squareTexture;
                case PointTextureType.diamond:
                    return CurveUIStatic.diamondTexture;
                case PointTextureType.plus:
                    return CurveUIStatic.plusTexture;
                default:
                    throw new KeyNotFoundException();
            }
        }

        public Composite Creator()
        {
            return _creator;
        }
    }
}
#endif
