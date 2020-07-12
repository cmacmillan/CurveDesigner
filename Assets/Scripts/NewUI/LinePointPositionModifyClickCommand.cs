using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class PointOnCurveClickCommand : IClickCommand
    {
        private CurveTrackingValue _point;
        private PositionCurveComposite _positionCurve;
        private IEnumerable<CurveTrackingValue> sampler;
        public PointOnCurveClickCommand(CurveTrackingValue point,PositionCurveComposite positionCurve,IEnumerable<CurveTrackingValue> sampler)
        {
            this.sampler = sampler;
            _point = point;
            _positionCurve = positionCurve;
        }

        void SetPosition(Curve3D curve,List<SelectableGUID> selected)
        {
            var oldDistance = _point.GetDistance(_positionCurve.positionCurve);
            var currentDistance = _positionCurve.PointClosestToCursor.distanceFromStartOfCurve;
            float change = currentDistance - oldDistance;
            var points = selected.GetSelected(sampler);
            foreach (var i in points)
            {
                var startingDistance = i.GetDistance(_positionCurve.positionCurve);
                i.SetDistance(startingDistance+change,_positionCurve.positionCurve);
            }
        }

        public void ClickDown(Vector2 mousePos,Curve3D curve,List<SelectableGUID> selected)
        {
            SetPosition(curve,selected);
        }

        public void ClickDrag(Vector2 mousePos, Curve3D curve, ClickHitData clicked,List<SelectableGUID> selected)
        {
            SetPosition(curve,selected);
        }

        public void ClickUp(Vector2 mousePos,Curve3D curve,List<SelectableGUID> selected)
        {
        }
    }
}
