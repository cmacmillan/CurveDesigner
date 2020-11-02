﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.NewUI
{
    public class CircleDraw : IDraw
    {
        private IComposite _creator;
        private Color _color;
        private Vector3 _center;
        private Vector3 _forward;
        private float _radius;
        private float _distanceToPoint;
        public CircleDraw(IComposite creator,Color color,Vector3 center, Vector3 forward,float radius)
        {
            _creator = creator;
            _color = color;
            _center = center;
            _forward = forward;
            _radius = radius;
            _distanceToPoint = GUITools.CameraDistanceToPoint(center);
        }
        public IComposite Creator()
        {
            return _creator;
        }

        public float DistFromCamera()
        {
            return _distanceToPoint + (int)IDrawSortLayers.Circles;
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
        private IComposite _creator;
        private Color _color;
        private Vector3 _center;
        private Vector3 _forward;
        private Vector3 _reference;
        private float _radius;
        private float _arc;
        private float _distanceToPoint;
        public ArcDraw(IComposite creator,Color color,Vector3 center, Vector3 forward,float radius,float arc,Vector3 reference)
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
        public IComposite Creator()
        {
            return _creator;
        }

        public float DistFromCamera()
        {
            return _distanceToPoint + (int)IDrawSortLayers.Circles;
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