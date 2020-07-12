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
        public PointOnCurveClickCommand(CurveTrackingValue point,PositionCurveComposite positionCurve)
        {
            _point = point;
            _positionCurve = positionCurve;
        }

        void SetPosition()
        {
            _point.SetDistance(_positionCurve.PointClosestToCursor.distanceFromStartOfCurve,_positionCurve.positionCurve);
        }

        public void ClickDown(Vector2 mousePos,Curve3D curve,List<SelectableGUID> selected)
        {
            SetPosition();
        }

        public void ClickDrag(Vector2 mousePos, Curve3D curve, ClickHitData clicked,List<SelectableGUID> selected)
        {
            SetPosition();
        }

        public void ClickUp(Vector2 mousePos,Curve3D curve,List<SelectableGUID> selected)
        {
        }
    }
}
