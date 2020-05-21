using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.NewUI
{
    public enum LineTextureType
    {
        Default = 0,
    }
    public class CurveSegmentDraw : IDraw
    {
        private Vector3 _startPoint;
        private Vector3 _endPoint;
        private Vector3 _startTangent;
        private Vector3 _endTangent;
        private LineTextureType _textureType;
        private Color _color;
        private float _width;
        private float _distanceToPoint;
        private IComposite _creator;
        public CurveSegmentDraw(IComposite creator,Vector3 startPoint, Vector3 endPoint, Vector3 startTangent, Vector3 endTangent,LineTextureType texture,Color lineColor,float width=5)
        {
            this._creator = creator;
            this._startPoint = startPoint;
            this._endPoint = endPoint;
            this._startTangent = startTangent;
            this._endTangent = endTangent;
            this._textureType = texture;
            this._color = lineColor;
            this._width = width;
            var avg = (_startPoint + _endPoint + _startTangent + _endTangent) / 4.0f;
            _distanceToPoint = GUITools.CameraDistanceToPoint(avg);
        }
        public float DistFromCamera()
        {
            return _distanceToPoint + (int)IDrawSortLayers.Curves;
        }

        public void Draw(DrawMode mode)
        {
            Handles.DrawBezier(_startPoint,_endPoint,_startTangent,_endTangent,_color,GetLineTextureByType(_textureType),_width);
        }
        public static Texture2D GetLineTextureByType(LineTextureType type)
        {
            switch (type)
            {
                case LineTextureType.Default:
                    return Curve3DSettings.defaultLineTexture;
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
