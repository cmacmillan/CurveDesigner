using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.NewUI
{
    class LineDraw : IDraw
    {
        private Vector3 _startPoint;
        private Vector3 _endPoint;
        private float _distanceToPoint;
        private IComposite _creator;
        private Color color;
        public LineDraw(IComposite creator,Vector3 startPoint,Vector3 endPoint,Color? color=null)
        {
            this._creator = creator;
            this._startPoint = startPoint;
            this._endPoint = endPoint;
            var avg = (_startPoint + _endPoint) / 2.0f;
            this._distanceToPoint = GUITools.CameraDistanceToPoint(avg);
            if (color.HasValue)
                this.color = color.Value;
            else
                this.color = Color.white;
        }

        public IComposite Creator()
        {
            return _creator;
        }

        public float DistFromCamera()
        {
            return _distanceToPoint + (int)IDrawSortLayers.Lines;
        }

        public void Draw(DrawMode mode,SelectionState selectionState)
        {
            Color beforeColor = Handles.color;
            Handles.color = color;
            Handles.DrawAAPolyLine(CurveSegmentDraw.GetLineTextureByType(LineTextureType.Default), new Vector3[] { _startPoint, _endPoint});
            Handles.color = beforeColor;
        }
    }
}
