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
        private List<CircleSizeComposite> points = new List<CircleSizeComposite>();
        public SizeCurveComposite(IComposite parent,FloatLinearDistanceSampler distanceSampler,Curve3D curve) : base(parent)
        {
            _splitterPoint = new SplitterPointComposite(this, curve, PointTextureType.circle,SizeCurveSplitCommandFactory.Instance, Curve3DSettings.Green);
            _distanceSampler = distanceSampler;
            foreach (var i in curve.sizeDistanceSampler.GetPointsBelowDistance(curve.positionCurve.GetLength()))
                points.Add(new CircleSizeComposite(this,i,curve.positionCurve));
        }

        public override void Click(Vector2 mousePosition, List<ClickHitData> clickHits)
        {
            _splitterPoint.FindSplitPoint();
            base.Click(mousePosition, clickHits);
        }

        public override void Draw(List<IDraw> drawList, ClickHitData clickedElement)
        {
            _splitterPoint.FindSplitPoint();
            base.Draw(drawList, clickedElement);
        }

        public override IEnumerable<IComposite> GetChildren()
        {
            yield return _splitterPoint;
            foreach (var i in points)
                yield return i;
        }
    }
}
