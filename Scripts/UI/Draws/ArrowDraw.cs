using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public class ArrowDraw : IDraw
    {
        private Composite _creator;
        private float _distFromCamera;
        private Vector3 _start;
        private Vector3 _direction;
        private Color _color;
        public ArrowDraw(Composite creator,Vector3 start,Vector3 direction,Color color)
        {
            _color = color;
            _creator = creator;
            _start = start;
            _direction = direction;
            GUITools.WorldToGUISpace(start, out _, out _distFromCamera);
        }
        public Composite Creator()
        {
            return _creator;
        }

        public float DistFromCamera()
        {
            return _distFromCamera + (int)DrawSortLayers.Lines;
        }
        private const float lineStartOffset = .06f;
        private const float coneSize = .2f;
        private const float lineLength = .6f;
        void DrawArrow(Color color, float alpha)
        {
            color.a *= alpha;
            Handles.color = color;
            var basePos = GUITools.WorldToHandleSpace(_start);
            float handleSize = HandleUtility.GetHandleSize(basePos)*lineLength;
            Vector3 lineStart = GUITools.WorldToHandleSpace(_start + lineStartOffset * handleSize*_direction);
            Vector3 lineEnd = GUITools.WorldToHandleSpace(_direction*handleSize+_start);
            Handles.DrawLine(lineStart, lineEnd);
            Vector3 dir = (lineEnd - lineStart).normalized;
            Handles.ConeHandleCap(-1, lineEnd, Quaternion.FromToRotation(Vector3.forward,dir), handleSize * coneSize, Event.current.type);
        }

        public void Draw(DrawMode mode, SelectionState selectionState)
        {
            DrawArrow(_color, 1);
        }
    }
}
