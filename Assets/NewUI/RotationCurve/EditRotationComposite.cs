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
        private FloatDistanceValue _point;
        private PointAlongCurveComposite _centerPoint;
        private PointComposite _rotationHandlePoint;
        private Curve3D _curve;

        public EditRotationComposite(IComposite parent,FloatDistanceValue value,Curve3D curve,Color color): base(parent)
        {
            _point = value;
            _curve = curve;
            _centerPoint = new PointAlongCurveComposite(this,value,curve,color);
            _rotationHandlePoint = new PointComposite(this, this, PointTextureType.diamond,new EditRotationClickCommand(), color);
        }

        public override void Draw(List<IDraw> drawList, ClickHitData clickedElement)
        {
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
                return point.reference.normalized*_curve.rotationDistanceSampler.GetAverageValue(_curve)+point.position;
            }
        }
    }
    public class EditRotationClickCommand : IClickCommand
    {
        public void ClickDown(Vector2 mousePos)
        {
        }

        public void ClickDrag(Vector2 mousePos, Curve3D curve, ClickHitData clicked)
        {
        }

        public void ClickUp(Vector2 mousePos)
        {
        }
    }
}
