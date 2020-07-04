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
        private FloatLinearDistanceSampler _distanceSampler;
        private List<EditRotationComposite> _points = new List<EditRotationComposite>();
        private SplitterPointComposite _splitterPoint = null;
        public RotationCurveComposite(IComposite parent,FloatLinearDistanceSampler distanceSampler,Curve3D curve,PositionCurveComposite positionCurveComposite) : base(parent)
        {
            _distanceSampler = distanceSampler;
            var blueColor = new Color(0,.8f,1.0f);
            _splitterPoint = new SplitterPointComposite(this, new TransformBlob(curve.transform,null), PointTextureType.circle, new ValueAlongCurveSplitCommand(curve,_distanceSampler,ValueAlongCurveSplitCommand.GetRotationCurve), blueColor,positionCurveComposite);
            foreach (var i in distanceSampler.GetPoints(curve))
                _points.Add(new EditRotationComposite(this,i,curve,distanceSampler,blueColor,positionCurveComposite));
        }

        public void DrawWindow(int[] selectedPoints, Curve3D curve)
        {
            if (selectedPoints.Length == 0)
                return;
            var primaryPointIndex = selectedPoints[selectedPoints.Length - 1];
            curve.rotationDistanceSampler.CacheOpenCurvePoints(curve.positionCurve);
            var points = curve.rotationDistanceSampler.GetPoints(curve);
            var primaryPoint = points[selectedPoints[selectedPoints.Length - 1]];
            primaryPoint.value = EditorGUILayout.FloatField("Rotation (degrees)",primaryPoint.value);
            primaryPoint.SetDistance(EditorGUILayout.FloatField("Distance along curve", primaryPoint.GetDistance(curve.positionCurve)), curve.positionCurve);
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
