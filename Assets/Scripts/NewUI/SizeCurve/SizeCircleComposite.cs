using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class SizeCircleComposite : IComposite
    {
        private BezierCurve _positionCurve;
        private List<PointComposite> ringPoints = new List<PointComposite>();
        private Curve3D _curve;
        public PointAlongCurveComposite linePoint;
        public FloatDistanceValue value;

        public const int ringPointCount=4;

        public SizeCircleComposite(IComposite parent,FloatDistanceValue value,BezierCurve positionCurve,Curve3D curve) : base(parent)
        {
            this.value = value;
            var purpleColor = new Color(.6f, .6f, .9f);
            linePoint = new PointAlongCurveComposite(this, value, curve,purpleColor);
            this._positionCurve = positionCurve;
            this._curve = curve;
            for (int i = 0; i < ringPointCount; i++)
            {
                var edgePointProvider = new SizeCircleEdgePointPositionProvider(value,i,curve);
                var clickCommmand = new SizeCurveEdgeClickCommand(value,edgePointProvider,this,curve);
                ringPoints.Add(new PointComposite(this,edgePointProvider,PointTextureType.diamond,clickCommmand,purpleColor));
            }
        }

        public override void Draw(List<IDraw> drawList, ClickHitData clickedElement)
        {
            linePoint.GetPositionForwardAndReference(out Vector3 position, out Vector3 forward,out Vector3 reference);
            drawList.Add(new CircleDraw(this,Color.white,_curve.transform.TransformPoint(position),_curve.transform.TransformDirection(forward),value.value+_curve.curveRadius));
            base.Draw(drawList, clickedElement);
        }

        public override IEnumerable<IComposite> GetChildren()
        {
            yield return linePoint;
            foreach (var i in ringPoints)
                yield return i;
        }
    }
    public static class CirclePlaneTools
    {
        private static Vector2 GetClosestPoint(Vector2 lineDirection, Vector2 lineOrigin, Vector2 point)
        {
            lineDirection = lineDirection.normalized;
            Vector2 normal = new Vector2(-lineDirection.y,lineDirection.x).normalized;
            if (lineDirection.x == 0)
                throw new NotImplementedException();
            var rs = lineDirection.y / lineDirection.x;
            var rt = normal.y / normal.x;
            var x = (-lineOrigin.y - rt * point.x + point.y+rs*lineOrigin.x) /(rs-rt);
            var y = rs * (x - lineOrigin.x) + lineOrigin.y;
            return new Vector2(x, y);
        }
        public static bool GetCursorPointOnPlane(PointAlongCurveComposite linePoint,out Vector3 cursorHitPosition, out Vector3 centerPoint, out Vector3 centerForward, out Vector3 centerReference, Curve3D curve)
        {
            Camera sceneCam = UnityEditor.SceneView.lastActiveSceneView.camera;
            Vector2 mousePos = Event.current.mousePosition;
            Ray cursorRay = sceneCam.ScreenPointToRay(GUITools.GuiSpaceToScreenSpace(mousePos));
            linePoint.GetPositionForwardAndReference(out centerPoint, out centerForward,out centerReference);
            centerForward = curve.transform.TransformDirection(centerForward);
            centerReference = curve.transform.TransformDirection(centerReference);
            centerPoint = curve.transform.TransformPoint(centerPoint);
            Plane circlePlane = new Plane(centerForward,centerPoint);
            bool result = circlePlane.Raycast(cursorRay, out float enter);
            if (result)
                cursorHitPosition = cursorRay.GetPoint(enter);
            else
                cursorHitPosition = Vector3.zero;
            return result;
        }
    }
    public class SizeCurveEdgeClickCommand : IClickCommand
    {
        private FloatDistanceValue _ring;
        private SizeCircleEdgePointPositionProvider _point;
        private SizeCircleComposite _owner;
        private Curve3D curve;

        public SizeCurveEdgeClickCommand(FloatDistanceValue ring, SizeCircleEdgePointPositionProvider point,SizeCircleComposite owner,Curve3D curve)
        {
            this._owner = owner;
            this._ring = ring;
            this._point = point;
            this.curve = curve;
        }

        void Set()
        {
            _owner.linePoint.GetPositionForwardAndReference(out Vector3 centerPoint, out Vector3 centerForward,out Vector3 centerReference);
            centerPoint = curve.transform.TransformPoint(centerPoint);
            Camera sceneCam = UnityEditor.SceneView.lastActiveSceneView.camera;
            var screenRay = sceneCam.ScreenPointToRay(GUITools.GuiSpaceToScreenSpace(Event.current.mousePosition));
            Vector3 pos = GUITools.GetClosestPointBetweenTwoLines(screenRay.origin,screenRay.direction,centerPoint,_point.Position-centerPoint);
            _ring.value = Vector3.Distance(pos,centerPoint)-curve.curveRadius;
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
                return curve.transform.TransformPoint(curve.positionCurve.GetPointAtDistance(_ring.GetDistance(curve.positionCurve)).GetRingPoint(360.0f * _ringPointIndex / (float)SizeCircleComposite.ringPointCount, _ring.value + curve.curveRadius));
            }
        }
    }
}
