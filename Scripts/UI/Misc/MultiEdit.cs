using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
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
    public class FloatSamplerOffsetModification
    {
        private float distanceAlongCurveOffset;
        private float valueOffset;
        public FloatSamplerOffsetModification(float distanceAlongCurveOffset,float valueOffset)
        {
            this.distanceAlongCurveOffset = distanceAlongCurveOffset;
            this.valueOffset = valueOffset;
        }
    }
}
