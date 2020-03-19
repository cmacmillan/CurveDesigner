using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class EditRotationComposite : IComposite, IPositionProvider
    {
        public FloatDistanceValue _point;
        public PointAlongCurveComposite _centerPoint;
        private PointComposite _rotationHandlePoint;
        private Curve3D _curve;

        public EditRotationComposite(IComposite parent,FloatDistanceValue value,Curve3D curve,Color color): base(parent)
        {
            _point = value;
            _curve = curve;
            _centerPoint = new PointAlongCurveComposite(this,value,curve,color);
            _rotationHandlePoint = new PointComposite(this, this, PointTextureType.diamond,new EditRotationClickCommand(this), color);
        }

        public override void Draw(List<IDraw> drawList, ClickHitData clickedElement)
        {
            _centerPoint.GetPositionForwardAndReference(out Vector3 circlePosition, out Vector3 circleForward,out Vector3 circleReference);
            drawList.Add(new CircleDraw(this,Color.white,circlePosition,circleForward,_curve.averageSize));
            drawList.Add(new LineDraw(this,_centerPoint.Position,Position));
            base.Draw(drawList, clickedElement);
        }

        public override IEnumerable<IComposite> GetChildren()
        {
            yield return _centerPoint;
            yield return _rotationHandlePoint;
        }

        public Vector3 Position {
            get
            {
                var point = _curve.positionCurve.GetPointAtDistance(_point.DistanceAlongCurve);
                return Quaternion.AngleAxis(_point.value,point.tangent)*(point.reference.normalized*_curve.averageSize)+point.position;
            }
        }
    }
    public class EditRotationClickCommand : IClickCommand
    {
        private EditRotationComposite _owner;
        public EditRotationClickCommand(EditRotationComposite owner)
        {
            _owner = owner;
        }
        private void Set()
        {
            if (CirclePlaneTools.GetCursorPointOnPlane(_owner._centerPoint, out Vector3 cursorHitPosition, out Vector3 centerPoint, out Vector3 centerForward,out Vector3 centerReference))
                _owner._point.value = Vector3.SignedAngle(centerReference,cursorHitPosition-centerPoint,centerForward);
        }
        public void ClickDown(Vector2 mousePos)
        {
            Set();
        }

        public void ClickDrag(Vector2 mousePos, Curve3D curve, ClickHitData clicked)
        {
            Set();
        }

        public void ClickUp(Vector2 mousePos)
        {
            Set();
        }
    }
}
