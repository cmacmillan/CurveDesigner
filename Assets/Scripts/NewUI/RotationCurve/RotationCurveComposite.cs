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
        private Old_FloatLinearDistanceSampler _distanceSampler;
        private List<EditRotationComposite> _points = new List<EditRotationComposite>();
        private SplitterPointComposite _splitterPoint = null;
        public RotationCurveComposite(IComposite parent,Old_FloatLinearDistanceSampler distanceSampler,Curve3D curve,PositionCurveComposite positionCurveComposite) : base(parent)
        {
            _distanceSampler = distanceSampler;
            var blueColor = new Color(0,.8f,1.0f);
            _splitterPoint = new SplitterPointComposite(this, new TransformBlob(curve.transform,null), PointTextureType.circle, new ValueAlongCurveSplitCommand(curve,_distanceSampler,ValueAlongCurveSplitCommand.GetRotationCurve), blueColor,positionCurveComposite);
            foreach (var i in distanceSampler.GetPoints(curve))
                _points.Add(new EditRotationComposite(this,i,curve,distanceSampler,blueColor,positionCurveComposite));
        }

        //pass it a FloatLinearDistanceSampler, name for the float field
        public void DrawWindow(Curve3D curve)
        {
            WindowDrawer.Draw(curve.rotationDistanceSampler.GetPoints(curve), curve);
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
