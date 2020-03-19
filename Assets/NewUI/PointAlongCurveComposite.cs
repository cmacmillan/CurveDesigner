using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class PointAlongCurveComposite : IComposite, IPositionProvider
    {
        public ILinePoint value;
        private PointComposite _point;
        private BezierCurve _positionCurve;

        public PointAlongCurveComposite(IComposite parent,FloatDistanceValue value,Curve3D curve,Color color) : base(parent)
        {
            this.value = value;
            _point = new PointComposite(this, this, PointTextureType.square, new LinePointPositionClickCommand(value, curve),color);
            _positionCurve = curve.positionCurve;
        }

        public Vector3 Position {
            get
            {
                GetPositionAndForward(out Vector3 position, out Vector3 forward);
                return position;
            }
        }

        public void GetPositionAndForward(out Vector3 position, out Vector3 forward)
        {
            var point = _positionCurve.GetPointAtDistance(value.DistanceAlongCurve);
            position = point.position;
            forward = point.tangent;
        }

        public override IEnumerable<IComposite> GetChildren()
        {
            yield return _point;
        }
    }
}
