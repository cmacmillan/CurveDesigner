#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public class CircleDraw : IDraw
    {
        private Composite _creator;
        private Color _color;
        private Vector3 _center;
        private Vector3 _forward;
        private float _radius;
        private float _distanceToPoint;
        public CircleDraw(Composite creator,Color color,Vector3 center, Vector3 forward,float radius)
        {
            _creator = creator;
            _color = color;
            _center = center;
            _forward = forward;
            _radius = radius;
            _distanceToPoint = GUITools.CameraDistanceToPoint(center);
        }
        public Composite Creator()
        {
            return _creator;
        }

        public float DistFromCamera()
        {
            return _distanceToPoint + (int)DrawSortLayers.Circles;
        }

        public void Draw(DrawMode mode,SelectionState selectionState)
        {
            Color beforeColor = Handles.color;
            Handles.color = _color;
            Handles.DrawWireDisc(_center, _forward, _radius);
            Handles.color = beforeColor;
        }
    }
    public class ArcDraw : IDraw
    {
        private Composite _creator;
        private Color _color;
        private Vector3 _center;
        private Vector3 _forward;
        private Vector3 _reference;
        private float _radius;
        private float _arc;
        private float _distanceToPoint;
        public ArcDraw(Composite creator,Color color,Vector3 center, Vector3 forward,float radius,float arc,Vector3 reference)
        {
            _creator = creator;
            _color = color;
            _center = center;
            _forward = forward;
            _reference = reference;
            _radius = radius;
            _arc = arc;
            _distanceToPoint = GUITools.CameraDistanceToPoint(center);
        }
        public Composite Creator()
        {
            return _creator;
        }

        public float DistFromCamera()
        {
            return _distanceToPoint + (int)DrawSortLayers.Circles;
        }

        public void Draw(DrawMode mode,SelectionState selectionState)
        {
            Color beforeColor = Handles.color;
            Handles.color = _color;
            Handles.DrawSolidArc(_center,_forward,_reference,_arc,_radius);
            Handles.color = beforeColor;
        }
    }
}
#endif
