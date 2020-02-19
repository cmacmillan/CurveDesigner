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
        public LineDraw(Vector3 startPoint,Vector3 endPoint)
        {
            this._startPoint = startPoint;
            this._endPoint = endPoint;
            var avg = (_startPoint + _endPoint) / 2.0f;
            this._distanceToPoint = GUITools.CameraDistanceToPoint(avg);
        }
        public float DistFromCamera()
        {
            return _distanceToPoint + (int)IDrawSortLayers.Lines;
        }

        public void Draw()
        {
            Handles.DrawAAPolyLine(CurveSegmentDraw.GetLineTextureByType(LineTextureType.Default), new Vector3[] { _startPoint, _endPoint});
        }
    }
}
