using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public class NormalPointComposite : Composite//, IPositionProvider
    {
        public NormalSamplerPoint _point;
        public PointAlongCurveComposite centerPoint;
        public Curve3D _curve;
        public override SelectableGUID GUID => _point.GUID;
        //should probably call positionCurve.recalculate when these points are modified, since this will affect the reference vectors
        //public Vector3 Position => throw new NotImplementedException();
        public NormalPointComposite(Composite parent, NormalSamplerPoint value, Curve3D curve, NormalSampler sampler, Color color, PositionCurveComposite positionCurveComposite) : base(parent)
        {
            _point = value;
            _curve = curve;
            centerPoint = new PointAlongCurveComposite(this, value, positionCurveComposite, color, _point.GUID, sampler);
        }
        public override IEnumerable<Composite> GetChildren()
        {
            yield return centerPoint;
        }
        private const float arrowLength = 3;
        public override void Draw(List<IDraw> drawList, ClickHitData closestElementToCursor)
        {
            Vector3 start = _curve.GetPositionAtDistanceAlongCurve(_point.GetDistance(_curve.positionCurve));
            drawList.Add(new ArrowDraw(this,start,_curve.transform.TransformDirection(_point.value).normalized,Color.white));
            base.Draw(drawList, closestElementToCursor);
        }
    }
}
