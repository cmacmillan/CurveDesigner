#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public class CurveSegmentDraw : IDraw
    {
        private Vector3 _startPoint;
        private Vector3 _endPoint;
        private Vector3 _startTangent;
        private Vector3 _endTangent;
        private Color _color;
        private float _width;
        private float _distanceToPoint;
        private Composite _creator;
        public CurveSegmentDraw(Composite creator,Vector3 startPoint, Vector3 endPoint, Vector3 startTangent, Vector3 endTangent,Color lineColor,float width=5)
        {
            this._creator = creator;
            this._startPoint = startPoint;
            this._endPoint = endPoint;
            this._startTangent = startTangent;
            this._endTangent = endTangent;
            this._color = lineColor;
            this._width = width;
            var avg = (_startPoint + _endPoint + _startTangent + _endTangent) / 4.0f;
            _distanceToPoint = GUITools.CameraDistanceToPoint(avg);
        }

        public float DistFromCamera()
        {
            return _distanceToPoint + (int)DrawSortLayers.Curves;
        }

        public void Draw(DrawMode mode,SelectionState selectionState)
        {
            Handles.DrawBezier(_startPoint,_endPoint,_startTangent,_endTangent,_color,CurveUIStatic.defaultLineTexture,_width);
        }

        public Composite Creator()
        {
            return _creator;
        }
    }
}
#endif
