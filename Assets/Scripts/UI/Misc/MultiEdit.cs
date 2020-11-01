using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class PointGroupOffsetModification
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
    }
    public class FloatDistanceSamplerOffsetModification
    {
        private float distanceAlongCurveOffset;
        private float valueOffset;
        public FloatDistanceSamplerOffsetModification(float distanceAlongCurveOffset,float valueOffset)
        {
            this.distanceAlongCurveOffset = distanceAlongCurveOffset;
            this.valueOffset = valueOffset;
        }
    }
}
