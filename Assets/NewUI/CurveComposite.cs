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
        private Curve3D _curve;
        public CurveComposite(Curve3D curve)
        {
            _positionCurve = new PositionCurveComposite(curve.positionCurve);
            this._curve = curve;
        }

        public override void Draw(List<IDraw> drawList)
        {
            for (int i = 0; i < _curve.positionCurve.NumSegments; i++)
            {
                var point1 = _curve.transform.TransformPoint(_curve.positionCurve[i, 0]);
                var point2 = _curve.transform.TransformPoint(_curve.positionCurve[i, 3]);
                var tangent1 = _curve.transform.TransformPoint(_curve.positionCurve[i, 1]);
                var tangent2 = _curve.transform.TransformPoint(_curve.positionCurve[i, 2]);
                drawList.Add(new CurveSegmentDraw(this,point1,point2,tangent1,tangent2,LineTextureType.Default,new Color(.6f, .6f, .6f)));
            }
            base.Draw(drawList);
        }

        public override IEnumerable<IComposite> GetChildren()
        {
            yield return _positionCurve;
        }
    }
}
