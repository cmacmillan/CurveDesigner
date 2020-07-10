using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public interface IMultiEditOffsetModification
    {
        void Apply(ISelectable target,Curve3D curve);
    }
    public class PointGroupOffsetModification : IMultiEditOffsetModification
    {
        private bool? isLocked=null;
        private Vector3 positionOffset;
        private Vector3 leftTangentOffset;
        private Vector3 rightTangentOffset;
        public PointGroupOffsetModification(bool? isLocked,Vector3 positionOffset,Vector3 leftTangentOffset,Vector3 rightTangentOffset)
        {
            this.isLocked = isLocked;
            this.positionOffset = positionOffset;
            this.rightTangentOffset = rightTangentOffset;
            this.leftTangentOffset = leftTangentOffset;
        }
        public void Apply(ISelectable targetArg,Curve3D curve)
        {
            var target = targetArg as PointGroup;
            if (isLocked.HasValue)
                target.SetPointLocked(isLocked.Value);
            target.SetWorldPositionByIndex(PGIndex.Position,target.GetWorldPositionByIndex(PGIndex.Position,curve.lockToPositionZero)+positionOffset,curve.lockToPositionZero);
            target.SetWorldPositionByIndex(PGIndex.LeftTangent,target.GetWorldPositionByIndex(PGIndex.LeftTangent,curve.lockToPositionZero)+leftTangentOffset,curve.lockToPositionZero);
            target.SetWorldPositionByIndex(PGIndex.RightTangent,target.GetWorldPositionByIndex(PGIndex.RightTangent,curve.lockToPositionZero)+rightTangentOffset,curve.lockToPositionZero);
        }
    }
    public class FloatDistanceSamplerOffsetModification : IMultiEditOffsetModification
    {
        private float distanceAlongCurveOffset;
        private float valueOffset;
        public FloatDistanceSamplerOffsetModification(float distanceAlongCurveOffset,float valueOffset)
        {
            this.distanceAlongCurveOffset = distanceAlongCurveOffset;
            this.valueOffset = valueOffset;
        }
        public void Apply(ISelectable targetArg, Curve3D curve)
        {
            var target = targetArg as FloatDistanceValue;
            target.value += valueOffset;
            var ogDistance = target.GetDistance(curve.positionCurve);
            target.SetDistance(ogDistance+distanceAlongCurveOffset,curve.positionCurve);
        }
    }
}
