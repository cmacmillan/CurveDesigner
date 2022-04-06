using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public class NormalCurveComposite : Composite, IValueAlongCurvePointProvider,IWindowDrawer
    {
        private NormalSampler _normalSampler;
        private List<NormalPointComposite> _points = new List<NormalPointComposite>();
        private SplitterPointComposite _splitterPoint;
        public NormalCurveComposite(Composite parent,NormalSampler sampler, Curve3D curve, PositionCurveComposite positionCurveComposite) : base(parent)
        {
            _normalSampler = sampler;
            Color col = Color.red;
            _splitterPoint = new SplitterPointComposite(this, positionCurveComposite.transformBlob, PointTextureType.circle, new ValueAlongCurveSplitCommand(curve,sampler,ValueAlongCurveSplitCommand.GetNormalCurve), col,positionCurveComposite);
            foreach (var i in _normalSampler.GetPoints(curve.positionCurve))
                _points.Add(new NormalPointComposite(this,i,curve,sampler,col,positionCurveComposite));
        }
        public override IEnumerable<Composite> GetChildren()
        {
            yield return _splitterPoint;
            foreach (var i in _points)
                yield return i;
        }
        public void DrawWindow(Curve3D curve)
        {
            WindowDrawer.Draw(curve.normalSampler.GetPoints(curve.positionCurve),curve);
        }
        public Clickable GetPointAtIndex(int index)
        {
            return _points[index].centerPoint.point;
        }
    }
}
