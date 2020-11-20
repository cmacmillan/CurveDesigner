using System.Collections.Generic;

namespace ChaseMacMillan.CurveDesigner
{
    public interface IOffsetProvider
    {
        float Offset { get; }
    }
    public class ThicknessPointComposite : IComposite, IOffsetProvider 
    {
        public SizeCircleComposite sizeCircle;
        private Curve3D curve;
        private FloatSamplerPoint point;
        private BezierCurve positionCurve;
        public ThicknessPointComposite(IComposite parent,FloatSamplerPoint point,BezierCurve positionCurve,Curve3D curve,PositionCurveComposite positionCurveComposite,FloatDistanceSampler sampler) : base(parent)
        {
            sizeCircle = new SizeCircleComposite(this, point, positionCurve, curve, positionCurveComposite,sampler,this);
            this.positionCurve = positionCurve;
            this.point = point;
            this.curve = curve;
        }

        public float Offset => curve.sizeSampler.GetValueAtDistance(point.GetDistance(positionCurve),positionCurve);

        public override IEnumerable<IComposite> GetChildren()
        {
            yield return sizeCircle;
        }
    }
}
