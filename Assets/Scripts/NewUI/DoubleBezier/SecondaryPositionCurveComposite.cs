using Assets.NewUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.NewUI
{
    public class SecondaryPositionCurveComposite : IComposite
    {
        public PositionCurveComposite positionCurve;
        public PointAlongCurveComposite centerPoint;
        public SecondaryPositionCurveComposite(IComposite parent,Curve3D curve,BezierCurveDistanceValue secondaryBezierCurve) : base (parent)
        {
            this.positionCurve = new PositionCurveComposite(this, curve, secondaryBezierCurve.secondaryCurve);
            centerPoint = new PointAlongCurveComposite(this,secondaryBezierCurve,curve,UnityEngine.Color.green);
        }
        public override IEnumerable<IComposite> GetChildren()
        {
            yield return positionCurve;
            yield return centerPoint;
        }
    }
}
