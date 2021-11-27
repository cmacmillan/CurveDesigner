#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public class RotationPointComposite : Composite, IPositionProvider
    {
        public FloatSamplerPoint _point;
        public PointAlongCurveComposite centerPoint;
        private PointComposite _rotationHandlePoint;
        public Curve3D _curve;
        public override SelectableGUID GUID => _point.GUID;

        public RotationPointComposite(Composite parent,FloatSamplerPoint value,Curve3D curve,FloatSampler sampler,Color color, PositionCurveComposite positionCurveComposite): base(parent)
        {
            _point = value;
            _curve = curve;
            centerPoint = new PointAlongCurveComposite(this,value,positionCurveComposite,color,_point.GUID,sampler);
            _rotationHandlePoint = new PointComposite(this, this, PointTextureType.diamond,new EditRotationClickCommand(this,value,sampler,curve), color,_point.GUID);
        }

        public override void Draw(List<IDraw> drawList, ClickHitData closestElementToCursor)
        {
            centerPoint.GetPositionForwardAndReference(out Vector3 circlePosition, out Vector3 circleForward,out Vector3 circleReference);
            drawList.Add(new CircleDraw(this,Color.white, _curve.transform.TransformPoint(circlePosition),_curve.transform.TransformDirection(circleForward),_curve.averageSize));
            drawList.Add(new LineDraw(this,centerPoint.Position,Position));
            base.Draw(drawList, closestElementToCursor);
        }

        public override IEnumerable<Composite> GetChildren()
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
        private RotationPointComposite _owner;
        private FloatSamplerPoint _value;
        private FloatSampler _sampler;
        private Curve3D _curve;
        private int Index {
            get
            {
                var points = _sampler.GetPoints(_curve.positionCurve);
                for (int i = 0; i < points.Count; i++)
                    if (points[i] == _value)
                        return i;
                throw new KeyNotFoundException();
            }
        }

        public EditRotationClickCommand(RotationPointComposite owner,FloatSamplerPoint value,FloatSampler sampler,Curve3D curve)
        {
            _owner = owner;
            _value = value;
            _sampler = sampler;
            _curve = curve;
        }
        private void Set(List<SelectableGUID> selected,Curve3D curve)
        {
            if (CirclePlaneTools.GetCursorPointOnPlane(_owner.centerPoint, out Vector3 cursorHitPosition, out Vector3 centerPoint, out Vector3 centerForward,out Vector3 centerReference,_curve))
            {
                var previousVector = _owner.GetVectorByAngle(_owner._curve.previousRotations[Index],out PointOnCurve point);
                float amountToRotate = Vector3.SignedAngle(_curve.transform.TransformDirection(previousVector),cursorHitPosition-centerPoint, centerForward);
                var selectedEditRotations = selected.GetSelected(_sampler.GetPoints(curve.positionCurve));
                foreach (var i in selectedEditRotations)
                    i.value += amountToRotate;
            }
        }
        public void ClickDown(Vector2 mousePos,Curve3D curve, List<SelectableGUID> selectedPoints)
        {
            Set(selectedPoints,curve);
        }

        public void ClickDrag(Vector2 mousePos, Curve3D curve, ClickHitData clicked, List<SelectableGUID> selectedPoints)
        {
            Set(selectedPoints,curve);
        }

        public void ClickUp(Vector2 mousePos,Curve3D curve, List<SelectableGUID> selectedPoints)
        {
            Set(selectedPoints,curve);
        }
    }
}
#endif
