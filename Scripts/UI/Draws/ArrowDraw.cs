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
        private float _arrowLength;
        public ArrowDraw(Composite creator,Vector3 start,Vector3 direction,Color color,float length)
        {
            _color = color;
            _creator = creator;
            _start = start;
            _direction = direction;
            _arrowLength = length;
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
        private const float coneSize = .5f;
        void DrawArrow(Color color, float alpha)
        {
            color.a *= alpha;
            Handles.color = color;
            var basePos = GUITools.WorldToHandleSpace(_start);
            Vector3 lineStart = GUITools.WorldToHandleSpace(_start + lineStartOffset * _arrowLength*_direction);
            Vector3 lineEnd = GUITools.WorldToHandleSpace(_direction*_arrowLength+_start);
            Handles.DrawLine(lineStart, lineEnd);
            Vector3 dir = (lineEnd - lineStart).normalized;
            Vector3 coneCenter = GUITools.WorldToHandleSpace(_direction*(_arrowLength-_arrowLength * coneSize*.5f)+ _start);
            Handles.ConeHandleCap(-1, coneCenter, Quaternion.FromToRotation(Vector3.forward,dir), Vector3.Distance(coneCenter,lineEnd), Event.current.type);
        }

        public void Draw(DrawMode mode, SelectionState selectionState)
        {
            DrawArrow(_color, 1);
        }
    }
}
