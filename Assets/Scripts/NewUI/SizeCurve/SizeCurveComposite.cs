using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class SizeCurveComposite : IComposite, IValueAlongCurvePointProvider, IWindowDrawer
    {
        private FloatDistanceSampler _distanceSampler;
        private SplitterPointComposite _splitterPoint = null;
        private List<SizeCircleComposite> points = new List<SizeCircleComposite>();
        public SizeCurveComposite(IComposite parent,FloatDistanceSampler distanceSampler,Curve3D curve,PositionCurveComposite positionCurveComposite) : base(parent)
        {
            _splitterPoint = new SplitterPointComposite(this, new TransformBlob(curve.transform,null), PointTextureType.circle,new ValueAlongCurveSplitCommand(curve,distanceSampler,ValueAlongCurveSplitCommand.GetSizeCurve), Curve3DSettings.Green,positionCurveComposite);
            _distanceSampler = distanceSampler;
            curve.positionCurve.Recalculate();
            foreach (var i in distanceSampler.GetPoints(curve.positionCurve))
                points.Add(new SizeCircleComposite(this,i,curve.positionCurve,curve,positionCurveComposite,_distanceSampler));
        }

        public void DrawWindow(Curve3D curve)
        {
            WindowDrawer.Draw(curve.sizeSampler.GetPoints(curve.positionCurve),curve);
        }

        public override IEnumerable<IComposite> GetChildren()
        {
            yield return _splitterPoint;
            foreach (var i in points)
                yield return i;
        }

        public IClickable GetPointAtIndex(int index)
        {
            return points[index].linePoint.point;
        }
    }
}
