#if UNITY_EDITOR
using System.Collections.Generic;

namespace ChaseMacMillan.CurveDesigner
{
    public class ThicknessPointComposite : Composite, IOffsetProvider 
    {
        public SizeCircleComposite sizeCircle;
        private Curve3D curve;
        private FloatSamplerPoint point;
        private BezierCurve positionCurve;
        public ThicknessPointComposite(Composite parent,FloatSamplerPoint point,BezierCurve positionCurve,Curve3D curve,PositionCurveComposite positionCurveComposite,FloatSampler sampler) : base(parent)
        {
            sizeCircle = new SizeCircleComposite(this, point, positionCurve, curve, positionCurveComposite,sampler,this);
            this.positionCurve = positionCurve;
            this.point = point;
            this.curve = curve;
        }

        public float Offset => curve.sizeSampler.GetValueAtDistance(point.GetDistance(positionCurve),positionCurve);

        public override IEnumerable<Composite> GetChildren()
        {
            yield return sizeCircle;
        }
    }
}
#endif
