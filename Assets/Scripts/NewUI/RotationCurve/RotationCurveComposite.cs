using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.NewUI
{
    public class RotationCurveComposite : IComposite, IValueAlongCurvePointProvider, IWindowDrawer
    {
        private FloatDistanceSampler _distanceSampler;
        private List<RotationPointComposite> _points = new List<RotationPointComposite>();
        private SplitterPointComposite _splitterPoint = null;
        public RotationCurveComposite(IComposite parent,FloatDistanceSampler distanceSampler,Curve3D curve,PositionCurveComposite positionCurveComposite) : base(parent)
        {
            _distanceSampler = distanceSampler;
            var blueColor = new Color(0,.8f,1.0f);
            _splitterPoint = new SplitterPointComposite(this, positionCurveComposite.transformBlob, PointTextureType.circle, new ValueAlongCurveSplitCommand(curve,_distanceSampler,ValueAlongCurveSplitCommand.GetRotationCurve), blueColor,positionCurveComposite);
            foreach (var i in distanceSampler.GetPoints(curve.positionCurve))
                _points.Add(new RotationPointComposite(this,i,curve,distanceSampler,blueColor,positionCurveComposite));
        }

        public void DrawWindow(Curve3D curve)
        {
            WindowDrawer.Draw(curve.rotationSampler.GetPoints(curve.positionCurve), curve);
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
