using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public class NormalComposite : Composite//, IPositionProvider
    {
        public NormalSamplerPoint _point;
        public PointAlongCurveComposite centerPoint;
        public Curve3D _curve;
        public override SelectableGUID GUID => _point.GUID;
        //public Vector3 Position => throw new NotImplementedException();
        public NormalComposite(Composite parent,NormalSamplerPoint value,Curve3D curve,NormalSampler sampler,Color color, PositionCurveComposite positionCurveComposite): base(parent)
        {
            _point = value;
            _curve = curve;
            centerPoint = new PointAlongCurveComposite(this, value, positionCurveComposite, color, _point.GUID,sampler);
        }
        public override IEnumerable<Composite> GetChildren()
        {
            yield return centerPoint;
        }
        public override void Draw(List<IDraw> drawList, ClickHitData closestElementToCursor)
        {
            base.Draw(drawList, closestElementToCursor);
        }
    }
}
