using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class EditColorComposite : IComposite
    {
        public PointAlongCurveComposite centerPoint;
        private ColorSamplerPoint _point;
        private Curve3D _curve;
        public override SelectableGUID GUID => _point.GUID;
        public EditColorComposite(IComposite parent,ColorSamplerPoint point,ColorDistanceSampler sampler,Color color,PositionCurveComposite positionCurveComposite,Curve3D curve) : base(parent)
        {
            _curve = curve;
            _point = point;
            centerPoint = new PointAlongCurveComposite(this,point,positionCurveComposite,color,point.GUID,sampler);
        }
        public override IEnumerable<IComposite> GetChildren()
        {
            yield return centerPoint;
        }
    }
}
