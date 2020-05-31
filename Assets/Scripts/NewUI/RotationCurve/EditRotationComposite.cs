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
        public PointAlongCurveComposite centerPoint;
        private PointComposite _rotationHandlePoint;
        public Curve3D _curve;

        public EditRotationComposite(IComposite parent,FloatDistanceValue value,Curve3D curve,FloatLinearDistanceSampler sampler,Color color): base(parent)
        {
            _point = value;
            _curve = curve;
            centerPoint = new PointAlongCurveComposite(this,value,curve,color);
            _rotationHandlePoint = new PointComposite(this, this, PointTextureType.diamond,new EditRotationClickCommand(this,value,sampler,curve), color);
        }

        public override void Draw(List<IDraw> drawList, ClickHitData clickedElement)
        {
            centerPoint.GetPositionForwardAndReference(out Vector3 circlePosition, out Vector3 circleForward,out Vector3 circleReference);
            drawList.Add(new CircleDraw(this,Color.white, _curve.transform.TransformPoint(circlePosition),_curve.transform.TransformDirection(circleForward),_curve.averageSize));
            drawList.Add(new LineDraw(this,centerPoint.Position,Position));
            base.Draw(drawList, clickedElement);
        }

        public override IEnumerable<IComposite> GetChildren()
        {
            yield return centerPoint;
            yield return _rotationHandlePoint;
        }

        public Vector3 GetVectorByAngle(float angle, out PointOnCurve point)
        {
            point = _curve.positionCurve.GetPointAtDistance(_point.GetDistance(_curve.positionCurve));
            return Quaternion.AngleAxis(angle, point.tangent) * (point.reference.normalized);
        }

        public Vector3 Position {
            get
            {
                return _curve.transform.TransformPoint(GetVectorByAngle(_point.value, out PointOnCurve point) * _curve.averageSize + point.position);
            }
        }
    }
    public class EditRotationClickCommand : IClickCommand
    {
        private EditRotationComposite _owner;
        private FloatDistanceValue _value;
        private FloatLinearDistanceSampler _sampler;
        private Curve3D _curve;
        private int Index {
            get
            {
                var points = _sampler.GetPoints(_curve);
                for (int i = 0; i < points.Count; i++)
                    if (points[i] == _value)
                        return i;
                throw new KeyNotFoundException();
            }
        }
        public EditRotationClickCommand(EditRotationComposite owner,FloatDistanceValue value,FloatLinearDistanceSampler sampler,Curve3D curve)
        {
            _owner = owner;
            _value = value;
            _sampler = sampler;
            _curve = curve;
        }
        private void Set()
        {
            if (CirclePlaneTools.GetCursorPointOnPlane(_owner.centerPoint, out Vector3 cursorHitPosition, out Vector3 centerPoint, out Vector3 centerForward,out Vector3 centerReference,_curve))
            {
                var previousVector = _owner.GetVectorByAngle(_owner._curve.previousRotations[Index],out PointOnCurve point);
                _owner._point.value += Vector3.SignedAngle(_curve.transform.TransformDirection(previousVector),cursorHitPosition-centerPoint,centerForward);
            }
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
