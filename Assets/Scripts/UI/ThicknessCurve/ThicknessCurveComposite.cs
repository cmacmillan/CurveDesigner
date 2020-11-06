using System.Collections.Generic;

namespace ChaseMacMillan.CurveDesigner
{
    public class ThicknessCurveComposite : IComposite, IValueAlongCurvePointProvider, IWindowDrawer
    {
        private FloatDistanceSampler _distanceSampler;
        private SplitterPointComposite _splitterPoint = null;
        private List<ThicknessPointComposite> points = new List<ThicknessPointComposite>();
        public ThicknessCurveComposite(IComposite parent, FloatDistanceSampler distanceSampler, Curve3D curve, PositionCurveComposite positionCurveComposite) : base(parent)
        {
            _splitterPoint = new SplitterPointComposite(this, positionCurveComposite.transformBlob, PointTextureType.circle, new ValueAlongCurveSplitCommand(curve, distanceSampler, ValueAlongCurveSplitCommand.GetThicknessCurve), Curve3DSettings.Green, positionCurveComposite);
            _distanceSampler = distanceSampler;
            foreach (var i in distanceSampler.GetPoints(curve.positionCurve))
                points.Add(new ThicknessPointComposite(this, i, curve.positionCurve, curve, positionCurveComposite, _distanceSampler));
        }

        public void DrawWindow(Curve3D curve)
        {
            WindowDrawer.Draw(curve.thicknessSampler.GetPoints(curve.positionCurve), curve);
        }

        public override IEnumerable<IComposite> GetChildren() 
        {
            yield return _splitterPoint;
            foreach (var i in points)
                yield return i;
        }

        public IClickable GetPointAtIndex(int index)
        {
            return points[index].sizeCircle.linePoint.point;
        }
    }
}
