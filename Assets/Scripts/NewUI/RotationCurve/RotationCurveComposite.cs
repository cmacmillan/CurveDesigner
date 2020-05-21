using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class RotationCurveComposite : IComposite, IValueAlongCurvePointProvider
    {
        private FloatLinearDistanceSampler _distanceSampler;
        private List<EditRotationComposite> _points = new List<EditRotationComposite>();
        private SplitterPointComposite _splitterPoint = null;
        public RotationCurveComposite(IComposite parent,FloatLinearDistanceSampler distanceSampler,Curve3D curve) : base(parent)
        {
            _distanceSampler = distanceSampler;
            var blueColor = new Color(0,.8f,1.0f);
            _splitterPoint = new SplitterPointComposite(this, curve, PointTextureType.circle, RotationCurveSplitCommandFactory.Instance, blueColor);
            foreach (var i in distanceSampler.GetPoints(curve))
                _points.Add(new EditRotationComposite(this,i,curve,distanceSampler,blueColor));
        }
        public override IEnumerable<IComposite> GetChildren()
        {
            yield return _splitterPoint;
            foreach (var i in _points)
                yield return i;
        }

        public IClickable GetPointAtIndex(int index)
        {
            return _points[index].centerPoint.point;
        }
    }
}
