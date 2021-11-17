#if UNITY_EDITOR
using System.Collections.Generic;

namespace ChaseMacMillan.CurveDesigner
{
    public class ThicknessCurveComposite : Composite, IValueAlongCurvePointProvider, IWindowDrawer
    {
        private FloatSampler _distanceSampler;
        private SplitterPointComposite _splitterPoint = null;
        private List<ThicknessPointComposite> points = new List<ThicknessPointComposite>();
        public ThicknessCurveComposite(Composite parent, FloatSampler distanceSampler, Curve3D curve, PositionCurveComposite positionCurveComposite) : base(parent)
        {
            _splitterPoint = new SplitterPointComposite(this, positionCurveComposite.transformBlob, PointTextureType.circle, new ValueAlongCurveSplitCommand(curve, distanceSampler, ValueAlongCurveSplitCommand.GetThicknessCurve), CurveUIStatic.Green, positionCurveComposite);
            _distanceSampler = distanceSampler;
            foreach (var i in distanceSampler.GetPoints(curve.positionCurve))
                points.Add(new ThicknessPointComposite(this, i, curve.positionCurve, curve, positionCurveComposite, _distanceSampler));
        }

        public void DrawWindow(Curve3D curve)
        {
            WindowDrawer.Draw(curve.thicknessSampler.GetPoints(curve.positionCurve), curve);
        }

        public override IEnumerable<Composite> GetChildren() 
        {
            yield return _splitterPoint;
            foreach (var i in points)
                yield return i;
        }

        public Clickable GetPointAtIndex(int index)
        {
            return points[index].sizeCircle.linePoint.point;
        }
    }
}
#endif
