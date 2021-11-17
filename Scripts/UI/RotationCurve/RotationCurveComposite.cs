#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public class RotationCurveComposite : Composite, IValueAlongCurvePointProvider, IWindowDrawer
    {
        private FloatSampler _distanceSampler;
        private List<RotationPointComposite> _points = new List<RotationPointComposite>();
        private SplitterPointComposite _splitterPoint = null;
        public RotationCurveComposite(Composite parent,FloatSampler distanceSampler,Curve3D curve,PositionCurveComposite positionCurveComposite) : base(parent)
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

        public override IEnumerable<Composite> GetChildren()
        {
            yield return _splitterPoint;
            foreach (var i in _points)
                yield return i;
        }

        public Clickable GetPointAtIndex(int index)
        {
            return _points[index].centerPoint.point;
        }
    }
}
#endif
