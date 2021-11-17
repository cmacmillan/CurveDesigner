#if UNITY_EDITOR
using System.Collections.Generic;

namespace ChaseMacMillan.CurveDesigner
{
    public class SizeCurveComposite : Composite, IValueAlongCurvePointProvider, IWindowDrawer
    {
        private FloatSampler _distanceSampler;
        private SplitterPointComposite _splitterPoint = null;
        private List<SizeCircleComposite> points = new List<SizeCircleComposite>();
        public SizeCurveComposite(Composite parent,FloatSampler distanceSampler,Curve3D curve,PositionCurveComposite positionCurveComposite) : base(parent)
        {
            _splitterPoint = new SplitterPointComposite(this, positionCurveComposite.transformBlob, PointTextureType.circle,new ValueAlongCurveSplitCommand(curve,distanceSampler,ValueAlongCurveSplitCommand.GetSizeCurve), CurveUIStatic.Green,positionCurveComposite);
            _distanceSampler = distanceSampler;
            foreach (var i in distanceSampler.GetPoints(curve.positionCurve))
                points.Add(new SizeCircleComposite(this,i,curve.positionCurve,curve,positionCurveComposite,_distanceSampler));
        }

        public void DrawWindow(Curve3D curve)
        {
            WindowDrawer.Draw(curve.sizeSampler.GetPoints(curve.positionCurve),curve);
        }

        public override IEnumerable<Composite> GetChildren()
        {
            yield return _splitterPoint;
            foreach (var i in points)
                yield return i;
        }

        public Clickable GetPointAtIndex(int index)
        {
            return points[index].linePoint.point;
        }
    }
}
#endif
