using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class ArcPointComposite : IComposite
    {
        private Curve3D _curve;
        private FloatSamplerPoint _point;
        public PointAlongCurveComposite centerPoint;
        private PointComposite _leftHandlePoint;
        private PointComposite _rightHandlePoint;
        private ArcPointPositionProvider _leftPosition;
        private ArcPointPositionProvider _rightPosition;
        public override SelectableGUID GUID => _point.GUID;
        public ArcPointComposite(IComposite parent,FloatSamplerPoint point,Curve3D curve,FloatDistanceSampler sampler,Color color, PositionCurveComposite positionCurveComposite): base(parent)
        {
            _curve = curve;
            _point = point;
            centerPoint = new PointAlongCurveComposite(this,point,positionCurveComposite,color,_point.GUID,sampler);

            _leftPosition = new ArcPointPositionProvider(curve, point, true);
            var leftClickCommand = new ArcPointClickCommand(this,point,sampler,curve,true);
            _leftHandlePoint = new PointComposite(this,_leftPosition,PointTextureType.diamond,leftClickCommand, color,_point.GUID);

            _rightPosition = new ArcPointPositionProvider(curve, point, false);
            var rightClickCommand = new ArcPointClickCommand(this,point,sampler,curve,false);
            _rightHandlePoint= new PointComposite(this,_rightPosition,PointTextureType.diamond,rightClickCommand, color,_point.GUID);
        }
        public override IEnumerable<IComposite> GetChildren()
        {
            yield return centerPoint;
            yield return _leftHandlePoint;
            yield return _rightHandlePoint;
        }
        public override void Draw(List<IDraw> drawList, ClickHitData closestElementToCursor)
        {
            centerPoint.GetPositionForwardAndReference(out Vector3 circlePosition, out Vector3 circleForward,out Vector3 circleReference);
            drawList.Add(new CircleDraw(this,Color.white, _curve.transform.TransformPoint(circlePosition),_curve.transform.TransformDirection(circleForward),_curve.averageSize));
            var reference = _curve.transform.TransformDirection(Quaternion.AngleAxis(180 - _point.value / 2 + _curve.rotationSampler.GetValueAtDistance(_point.GetDistance(_curve.positionCurve), _curve.positionCurve), circleForward) * circleReference);
            drawList.Add(new ArcDraw(this, new Color(1,1,1,.1f), _curve.transform.TransformPoint(circlePosition), _curve.transform.TransformDirection(circleForward), _curve.averageSize,_point.value,reference));
            drawList.Add(new LineDraw(this,centerPoint.Position,_leftPosition.Position));
            drawList.Add(new LineDraw(this,centerPoint.Position,_rightPosition.Position));
            base.Draw(drawList, closestElementToCursor);
        }
    }
    public class ArcPointPositionProvider : IPositionProvider
    {
        private Curve3D _curve;
        private bool _isLeft;
        private FloatSamplerPoint _point;
        public ArcPointPositionProvider(Curve3D curve,FloatSamplerPoint point, bool isLeft)
        {
            this._curve = curve;
            this._isLeft = isLeft;
            this._point = point;
        }
        public Vector3 Position
        {
            get
            {
                return _curve.transform.TransformPoint(GetVectorByArc(_point.value, _isLeft, out PointOnCurve point) * _curve.averageSize + point.position);
            }
        }
        public Vector3 GetVectorByArc(float arc, bool isLeft,out PointOnCurve point)
        {
            float angle = 180+(_isLeft?-1:1)*arc / 2 + _curve.rotationSampler.GetValueAtDistance(_point.GetDistance(_curve.positionCurve),_curve.positionCurve);
            point = _curve.positionCurve.GetPointAtDistance(_point.GetDistance(_curve.positionCurve));
            return Quaternion.AngleAxis(angle, point.tangent) * (point.reference.normalized);
        }
    }
    public class ArcPointClickCommand : IClickCommand
    {
        private ArcPointComposite _owner;
        private FloatSamplerPoint _point;
        private FloatDistanceSampler _sampler;
        private Curve3D _curve;
        private bool _isLeft;
        public ArcPointClickCommand(ArcPointComposite owner, FloatSamplerPoint point, FloatDistanceSampler sampler, Curve3D curve,bool isLeft)
        {
            this._owner = owner;
            this._point = point;
            this._sampler = sampler;
            this._curve = curve;
            this._isLeft = isLeft;
        }

        private void Set(List<SelectableGUID> selected,Curve3D curve)
        {
            if (CirclePlaneTools.GetCursorPointOnPlane(_owner.centerPoint, out Vector3 cursorHitPosition, out Vector3 centerPoint, out Vector3 centerForward,out Vector3 centerReference,_curve))
            {
                //var previousVector = _owner.GetVectorByAngle(_owner._curve.previousRotations[Index],out PointOnCurve point);
                float angle = _curve.rotationSampler.GetValueAtDistance(_point.GetDistance(_curve.positionCurve), _curve.positionCurve);
                var rotation = Quaternion.AngleAxis(angle,centerForward);
                //var reference = _curve.transform.TransformDirection(Quaternion.AngleAxis(-rotation, centerForward) * centerReference);
                float amountToRotate = Vector3.SignedAngle(rotation*centerReference,cursorHitPosition-centerPoint,centerForward);
                _point.value = 2 * (180 - Mathf.Abs(amountToRotate));
                /*
                var selectedEditRotations = selected.GetSelected(_sampler.GetPoints(curve.positionCurve));
                foreach (var i in selectedEditRotations)
                    i.value += amountToRotate;
                */
            }
        }
        public void ClickDown(Vector2 mousePos, Curve3D curve, List<SelectableGUID> selected)
        {
            Set(selected,curve);
        }

        public void ClickDrag(Vector2 mousePos, Curve3D curve, ClickHitData clicked, List<SelectableGUID> selected)
        {
            Set(selected,curve);
        }

        public void ClickUp(Vector2 mousePos, Curve3D curve, List<SelectableGUID> selected)
        {
            Set(selected,curve);
        }
    }
}
