using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class PointDraw : IDraw
    {
        Vector2 _guiPos;
        float _distFromCamera;
        int _size;
        PointTextureType type;
        public PointDraw(Vector3 position,PointTextureType type,int size = 5)
        {
            GUITools.WorldToGUISpace(position, out _guiPos, out _distFromCamera);
            this._size = size;
            this.type = type;
        }
        public float DistFromCamera()
        {
            return _distFromCamera;
        }
        public void Draw()
        {
            var rect = GUITools.GetRectCenteredAtPosition(_guiPos, _size, _size);
            DrawPoint(rect,Color.white,GetPointTexture(type));
        }
        private static void DrawPoint(Rect position, Color color, Texture2D tex)
        {
            Color oldColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(position, tex);
            GUI.color = oldColor;
        }
        private static Texture2D GetPointTexture(PointTextureType type)
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
    }
}
