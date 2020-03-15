using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class SizeCurveEdgeClickCommand : IClickCommand
    {
        public void ClickDown(Vector2 mousePos)
        {
            throw new NotImplementedException();
        }

        public void ClickDrag(Vector2 mousePos, Curve3D curve, ClickHitData clicked)
        {
            throw new NotImplementedException();
        }

        public void ClickUp(Vector2 mousePos)
        {
            throw new NotImplementedException();
        }
    }
    public class SizeCircleEdgePointPositionProvider : IPositionProvider
    {
        private int _ringPointIndex;
        private FloatDistanceValue _ring;
        private Curve3D curve;
        public SizeCircleEdgePointPositionProvider(FloatDistanceValue ring, int ringPointIndex,Curve3D curve)
        {
            this._ringPointIndex = ringPointIndex;
            this._ring = ring;
            this.curve = curve;
        }

        public Vector3 Position {
            get {
                return curve.positionCurve.GetPointAtDistance(_ring.Distance).GetRingPoint(360.0f*_ringPointIndex / (float)SizeCircleComposite.ringPointCount, _ring.value);
            }
            set
            {
                throw new NotImplementedException();
            }
        }
    }
    public class SizeCircleComposite : IComposite, IPositionProvider,ILinePoint
    {
        private FloatDistanceValue _point;
        private BezierCurve _positionCurve;
        private PointComposite centerPoint;

        public const int ringPointCount=4;

        private List<PointComposite> ringPoints = new List<PointComposite>();

        public SizeCircleComposite(IComposite parent,FloatDistanceValue value,BezierCurve positionCurve,Curve3D curve) : base(parent)
        {
            this._point = value;
            this._positionCurve = positionCurve;
            centerPoint = new PointComposite(this, this, PointTextureType.square, new LinePointPositionClickCommand(this,curve), new Color(.6f,.6f,.9f));
            for (int i = 0; i < ringPointCount; i++)
            {
                var edgePointProvider = new SizeCircleEdgePointPositionProvider(_point,i,curve);
                var clickCommmand = new SizeCurveEdgeClickCommand();
                ringPoints.Add(new PointComposite(this,edgePointProvider,PointTextureType.diamond,clickCommmand,new Color(.6f,.6f,.9f)));
            }
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
            foreach (var i in ringPoints)
                yield return i;
        }
    }
}
