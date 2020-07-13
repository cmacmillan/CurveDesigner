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
            bool isClosedLoop = _positionCurve.positionCurve.isClosedLoop;
            float length = _positionCurve.positionCurve.GetLength();
            //START this is all just to not squash multiselected points when you reach the edge of a curve
            float minDist = float.MaxValue;
            float maxDist = 0;
            if (!isClosedLoop)
                foreach (var i in points)
                {
                    var distance = i.GetDistance(_positionCurve.positionCurve);
                    minDist = Mathf.Min(minDist,distance);
                    maxDist = Mathf.Min(maxDist,distance);
                }
            if (change > 0)
                change = Mathf.Min(length - maxDist, change);
            else
                change = Mathf.Max(0-minDist, change);
            //////END
            foreach (var i in points)
            {
                var startingDistance = i.GetDistance(_positionCurve.positionCurve);
                if (isClosedLoop)
                    i.SetDistance(mod(startingDistance+change,length),_positionCurve.positionCurve);
                else
                    i.SetDistance(startingDistance+change,_positionCurve.positionCurve);
            }
        }

        static float mod(float x, float m)
        {
            return (x % m + m) % m;
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
