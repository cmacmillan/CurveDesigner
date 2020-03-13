using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class CircleSizeComposite : IComposite
    {
        private FloatDistanceValue _point;
        private BezierCurve _positionCurve;
        public CircleSizeComposite(IComposite parent,FloatDistanceValue value,BezierCurve positionCurve) : base(parent)
        {
            this._point = value;
            this._positionCurve = positionCurve;
        }

        private void GetPositionAndForward(out Vector3 position, out Vector3 forward)
        {
            var point = _positionCurve.GetPointAtDistance(_point.Distance);
            position = point.position;
            forward = point.tangent;
        }

        public override void Draw(List<IDraw> drawList, ClickHitData clickedElement)
        {
            GetPositionAndForward(out Vector3 position, out Vector3 forward);
            drawList.Add(new CircleDraw(this,Color.white,position,forward,_point.value));
            base.Draw(drawList, clickedElement);
        }
    }
}
