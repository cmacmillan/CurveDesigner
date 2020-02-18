using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class CurveComposite : IComposite
    {
        private PositionCurveComposite _positionCurve;
        public CurveComposite(Curve3D curve)
        {
            _positionCurve = new PositionCurveComposite(curve.positionCurve);
        }

        public override ClickHitData Click(Vector2 position)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IComposite> GetChildren()
        {
            yield return _positionCurve;
        }
    }
}
