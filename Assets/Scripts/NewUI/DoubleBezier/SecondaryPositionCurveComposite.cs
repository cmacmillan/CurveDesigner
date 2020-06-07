﻿using Assets.NewUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class SecondaryPositionCurveComposite : IComposite
    {
        public PositionCurveComposite positionCurve;
        public PointAlongCurveComposite centerPoint;
        private Curve3D _curve;
        private TransformBlob transformBlob;
        public SecondaryPositionCurveComposite(IComposite parent,Curve3D curve,BezierCurveDistanceValue secondaryBezierCurve) : base (parent)
        {
            var curveInfoAtCenterPoint = curve.positionCurve.GetPointAtDistance(secondaryBezierCurve.GetDistance(curve.positionCurve));
            //Matrix4x4 tangentSpaceToLocalSpace = Matrix4x4.Rotate(Quaternion.LookRotation(curveInfoAtCenterPoint.tangent,curveInfoAtCenterPoint.reference));//.inverse
            //tangentSpaceToLocalSpace = Matrix4x4.Translate(curveInfoAtCenterPoint.position)*tangentSpaceToLocalSpace;
            //tangentSpaceToLocalSpace = Matrix4x4.Translate(curveInfoAtCenterPoint.position);
            this._curve = curve; 
            centerPoint = new PointAlongCurveComposite(this, secondaryBezierCurve, positionCurve, UnityEngine.Color.green);
            transformBlob = new TransformBlob(curve.transform,new DynamicMatrix4x4(centerPoint));
            this.positionCurve = new PositionCurveComposite(this, curve, secondaryBezierCurve.secondaryCurve,new SecondaryPositionCurveSplitCommand(secondaryBezierCurve.secondaryCurve,curve),transformBlob);
        }
        public override IEnumerable<IComposite> GetChildren()
        {
            yield return positionCurve;
            yield return centerPoint;
        }
        public override void Draw(List<IDraw> drawList, ClickHitData clickedElement)
        {
            UICurve.GetCurveDraw(drawList,positionCurve.positionCurve,transformBlob,this);
            base.Draw(drawList, clickedElement);
        }
    }
}