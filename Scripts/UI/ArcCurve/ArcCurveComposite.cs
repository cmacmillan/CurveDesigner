#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public class ArcCurveComposite : Composite, IValueAlongCurvePointProvider, IWindowDrawer
    {
        private FloatSampler _distanceSampler;
        private List<ArcPointComposite> _points = new List<ArcPointComposite>();
        private SplitterPointComposite _splitterPoint = null;
        public ArcCurveComposite(Composite parent,FloatSampler distanceSampler,Curve3D curve,PositionCurveComposite positionCurveComposite) : base(parent)
        {
            _distanceSampler = distanceSampler;
            var blueColor = new Color(0,.8f,1.0f);
            _splitterPoint = new SplitterPointComposite(this, positionCurveComposite.transformBlob, PointTextureType.circle, new ValueAlongCurveSplitCommand(curve,_distanceSampler,ValueAlongCurveSplitCommand.GetArcCurve), blueColor,positionCurveComposite);
            foreach (var i in distanceSampler.GetPoints(curve.positionCurve))
                _points.Add(new ArcPointComposite(this,i,curve,distanceSampler,blueColor,positionCurveComposite));
        }

        public void DrawWindow(Curve3D curve)
        {
            WindowDrawer.Draw(curve.arcOfTubeSampler.GetPoints(curve.positionCurve), curve);
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
