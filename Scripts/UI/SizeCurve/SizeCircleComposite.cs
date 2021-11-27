#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public class SizeCircleComposite : Composite
    {
        private BezierCurve _positionCurve;
        private List<PointComposite> ringPoints = new List<PointComposite>();
        private Curve3D _curve;
        public PointAlongCurveComposite linePoint;
        public FloatSamplerPoint value;
        public FloatSampler _sampler;
        private IOffsetProvider offset;

        public const int ringPointCount=4;

        public override SelectableGUID GUID => value.GUID;

        public SizeCircleComposite(Composite parent,FloatSamplerPoint value,BezierCurve positionCurve,Curve3D curve,PositionCurveComposite positionCurveComposite,FloatSampler _sampler,IOffsetProvider offset = null) : base(parent)
        {
            this._sampler = _sampler;
            this.value = value;
            var purpleColor = new Color(.6f, .6f, .9f);
            linePoint = new PointAlongCurveComposite(this, value, positionCurveComposite,purpleColor,value.GUID,_sampler);
            this._positionCurve = positionCurve;
            this._curve = curve;
            this.offset = offset;
            for (int i = 0; i < ringPointCount; i++)
            {
                var edgePointProvider = new SizeCircleEdgePointPositionProvider(value,i,curve,offset);
                var clickCommmand = new SizeCurveEdgeClickCommand(value,edgePointProvider,this,curve,offset);
                ringPoints.Add(new PointComposite(this,edgePointProvider,PointTextureType.diamond,clickCommmand,purpleColor,value.GUID));
            }
        }

        public override void Draw(List<IDraw> drawList, ClickHitData closestElementToCursor)
        {
            linePoint.GetPositionForwardAndReference(out Vector3 position, out Vector3 forward,out Vector3 reference);
            if (offset != null)
            {
                drawList.Add(new CircleDraw(this,new Color(.8f,.8f,.8f),_curve.transform.TransformPoint(position),_curve.transform.TransformDirection(forward),(offset==null?0:offset.Offset))); 
            }
            drawList.Add(new CircleDraw(this,Color.white,_curve.transform.TransformPoint(position),_curve.transform.TransformDirection(forward),value.value+(offset==null?0:offset.Offset)));
            base.Draw(drawList, closestElementToCursor);
        }

        public override IEnumerable<Composite> GetChildren()
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
        private FloatSamplerPoint _ring;
        private SizeCircleEdgePointPositionProvider _point;
        private SizeCircleComposite _owner;
        private Curve3D curve;
        private IOffsetProvider offset;

        public SizeCurveEdgeClickCommand(FloatSamplerPoint ring, SizeCircleEdgePointPositionProvider point,SizeCircleComposite owner,Curve3D curve,IOffsetProvider offset =null)
        {
            this._owner = owner;
            this._ring = ring;
            this._point = point;
            this.offset = offset;
            this.curve = curve;
        }

        void Set(List<SelectableGUID> selectedPoints,Curve3D curve)
        {
            _owner.linePoint.GetPositionForwardAndReference(out Vector3 centerPoint, out Vector3 centerForward,out Vector3 centerReference);
            centerPoint = curve.transform.TransformPoint(centerPoint);
            Camera sceneCam = UnityEditor.SceneView.lastActiveSceneView.camera;
            var screenRay = sceneCam.ScreenPointToRay(GUITools.GuiSpaceToScreenSpace(Event.current.mousePosition));
            if (GUITools.GetClosestPointBetweenTwoLines(screenRay.origin, screenRay.direction, centerPoint, _point.Position - centerPoint, out Vector3 pos))
            {
                var sizeChange = (Vector3.Distance(pos, centerPoint)) - (_ring.value + (offset == null ? 0 : offset.Offset));
                var selectedSizePoints = selectedPoints.GetSelected(_owner._sampler.GetPoints(curve.positionCurve));
                float minChange = float.MaxValue;
                foreach (var i in selectedSizePoints)
                {
                    float newVal = Mathf.Max(0, i.value + sizeChange);
                    float change = newVal - i.value;
                    if (Mathf.Abs(change) < Mathf.Abs(minChange))
                        minChange = change;
                }
                foreach (var i in selectedSizePoints)
                    i.value += minChange;
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
    public class SizeCircleEdgePointPositionProvider : IPositionProvider
    {
        private int _ringPointIndex;
        private FloatSamplerPoint _ring;
        private Curve3D curve;
        private IOffsetProvider offset;
        public SizeCircleEdgePointPositionProvider(FloatSamplerPoint ring, int ringPointIndex,Curve3D curve,IOffsetProvider offset = null)
        {
            this._ringPointIndex = ringPointIndex;
            this._ring = ring;
            this.curve = curve;
            this.offset = offset;
        }

        public Vector3 Position {
            get {
                return curve.transform.TransformPoint(curve.positionCurve.GetPointAtDistance(_ring.GetDistance(curve.positionCurve)).GetRingPoint(360.0f * _ringPointIndex / (float)SizeCircleComposite.ringPointCount, _ring.value+(offset==null?0:offset.Offset)));
            }
        }
    }
}
#endif
