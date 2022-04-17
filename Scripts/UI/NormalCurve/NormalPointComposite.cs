using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public class NormalPointComposite : Composite, IPositionProvider
    {
        public NormalSamplerPoint _point;
        public PointAlongCurveComposite centerPoint;
        public PointComposite edgePointComposite;
        public Curve3D _curve;
        public override SelectableGUID GUID => _point.GUID;

        public Vector3 Position
        {
            get
            {
                Vector3 start = _curve.GetPositionAtDistanceAlongCurve(_point.GetDistance(_curve.positionCurve));
                float size = HandleUtility.GetHandleSize(start)*compositeSize;
                return start + _point.value*size;
            }
        }

        //should probably call positionCurve.recalculate when these points are modified, since this will affect the reference vectors
        public NormalPointComposite(Composite parent, NormalSamplerPoint value, Curve3D curve, NormalSampler sampler, Color color, PositionCurveComposite positionCurveComposite) : base(parent)
        {
            _point = value;
            _curve = curve;
            centerPoint = new PointAlongCurveComposite(this, value, positionCurveComposite, color, _point.GUID, sampler);
            edgePointComposite = new PointComposite(this,this, PointTextureType.diamond,new EditNormalClickCommand(this,_point,sampler,curve),Color.red,GUID);
        }
        public override IEnumerable<Composite> GetChildren()
        {
            yield return centerPoint;
            yield return edgePointComposite;
        }
        public float GetSize(out Vector3 start)
        {
            start = _curve.GetPositionAtDistanceAlongCurve(_point.GetDistance(_curve.positionCurve));
            return HandleUtility.GetHandleSize(start) * compositeSize;
        }
        private const float compositeSize= .5f;
        public override void Draw(List<IDraw> drawList, ClickHitData closestElementToCursor)
        {
            float size = GetSize(out Vector3 start);
            drawList.Add(new CircleDraw(this, Color.white, start, Vector3.right,size));
            drawList.Add(new CircleDraw(this, Color.white, start, Vector3.up,size));
            drawList.Add(new CircleDraw(this, Color.white, start, Vector3.forward,size));
            drawList.Add(new ArrowDraw(this,start,_curve.transform.TransformDirection(_point.value).normalized,Color.white,size));
            base.Draw(drawList, closestElementToCursor);
        }
    }
    public class EditNormalClickCommand : IClickCommand
    {
        private NormalPointComposite _owner;
        private NormalSamplerPoint _value;
        private NormalSampler _sampler;
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

        public EditNormalClickCommand(NormalPointComposite owner,NormalSamplerPoint value, NormalSampler sampler,Curve3D curve)
        {
            _owner = owner;
            _value = value;
            _sampler = sampler;
            _curve = curve;
        }

        public static bool RaycastSphere(Vector3 center,float radius, Ray r, out Vector3 hitPos)
        {
            //https://en.wikipedia.org/wiki/Line%E2%80%93sphere_intersection
            Vector3 oMinC = r.origin - center;
            float left = Vector3.Dot(r.direction, oMinC);
            float leftSqr = left * left;
            float mag = oMinC.magnitude;
            float right = mag * mag - radius * radius;
            float det = leftSqr - right;
            if (det < 0)
            {
                hitPos = Vector3.zero;
                return false;
            }
            float near = -left - Mathf.Sqrt(det);
            hitPos = r.GetPoint(near);
            return true;
        }

        private void Set(List<SelectableGUID> selected,Curve3D curve)
        {
            var ray = GUITools.GetCursorRay();
            float radius = _owner.GetSize(out Vector3 center);
            if (RaycastSphere(center,radius,ray,out Vector3 hit))
            {
                Vector3 worldOffset = hit - center;
                Vector3 local = _curve.transform.InverseTransformDirection(worldOffset);
                _value.value = local.normalized;//normalization prolly not absolutely necessary here
                _curve.positionCurve.Recalculate();
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
