#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public class PointOnCurveClickCommand : IClickCommand
    {
        private ISamplerPoint _point;
        private PositionCurveComposite _positionCurve;
        private ISampler sampler;

        public PointOnCurveClickCommand(ISamplerPoint point,PositionCurveComposite positionCurve,ISampler sampler)
        {
            this.sampler = sampler;
            _point = point;
            _positionCurve = positionCurve;
        }

        public static void ClampOffset(float change,Curve3D curve, IEnumerable<ISamplerPoint> selectedPoints)
        {
            if (!curve.IsClosedLoop)
            {
                float minDist = float.MaxValue;
                float maxDist = 0;
                foreach (var i in selectedPoints)
                {
                    var distance = i.GetDistance(curve.positionCurve);
                    minDist = Mathf.Min(minDist, distance);
                    maxDist = Mathf.Max(maxDist, distance);
                }
                if (change > 0)
                    change = Mathf.Min(curve.positionCurve.GetLength() - maxDist, change);
                else
                    change = Mathf.Max(0 - minDist, change);
            }
            float length = curve.positionCurve.GetLength();
            foreach (var i in selectedPoints)
            {
                var startingDistance = i.GetDistance(curve.positionCurve);
                if (curve.IsClosedLoop)
                    i.SetDistance(Utils.ModFloat(startingDistance+change,length),curve.positionCurve);
                else
                    i.SetDistance(startingDistance+change,curve.positionCurve);
            }
        }

        void SetPosition(Curve3D curve,List<SelectableGUID> selected)
        {
            var oldDistance = _point.GetDistance(_positionCurve.positionCurve);
            var currentDistance = _positionCurve.PointClosestToCursor.distanceFromStartOfCurve;
            float change = currentDistance - oldDistance;
            var points = selected.GetSelected(sampler.GetPoints(curve.positionCurve));
            bool isClosedLoop = _positionCurve.positionCurve.isClosedLoop;
            float length = _positionCurve.positionCurve.GetLength();
            ClampOffset(change, curve,points);
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
#endif
