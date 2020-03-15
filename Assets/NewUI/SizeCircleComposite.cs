using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class SizeCircleComposite : IComposite, IPositionProvider,ILinePoint
    {
        private FloatDistanceValue _point;
        private BezierCurve _positionCurve;
        private PointComposite centerPoint;

        public SizeCircleComposite(IComposite parent,FloatDistanceValue value,BezierCurve positionCurve,Curve3D uiCurve) : base(parent)
        {
            this._point = value;
            this._positionCurve = positionCurve;
            centerPoint = new PointComposite(this, this, PointTextureType.square, new LinePointPositionModifyClickCommand(this,uiCurve), new Color(.6f,.6f,.9f));
        }

        public Vector3 Position {
            get {
                GetPositionAndForward(out Vector3 position, out Vector3 forward);
                return position;
            }
            set => throw new NotImplementedException();
        }

        public float DistanceAlongCurve
        {
            get { return _point.Distance; }
            set { _point.Distance = value; }
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

        public override IEnumerable<IComposite> GetChildren()
        {
            yield return centerPoint;
        }
    }
}
