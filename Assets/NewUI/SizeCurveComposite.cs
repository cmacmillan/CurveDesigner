using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class SizeCurveComposite : IComposite
    {
        private FloatLinearDistanceSampler _distanceSampler;
        private SplitterPointComposite _splitterPoint = null;
        private List<SizeCircleComposite> points = new List<SizeCircleComposite>();
        public SizeCurveComposite(IComposite parent,FloatLinearDistanceSampler distanceSampler,Curve3D curve) : base(parent)
        {
            _splitterPoint = new SplitterPointComposite(this, curve, PointTextureType.circle,SizeCurveSplitCommandFactory.Instance, Curve3DSettings.Green);
            _distanceSampler = distanceSampler;
            curve.positionCurve.Recalculate();
            foreach (var i in curve.sizeDistanceSampler.GetPointsBelowDistance(curve.positionCurve.GetLength()))
                points.Add(new SizeCircleComposite(this,i,curve.positionCurve,curve));
        }

        public override IEnumerable<IComposite> GetChildren()
        {
            yield return _splitterPoint;
            foreach (var i in points)
                yield return i;
        }
    }
}
