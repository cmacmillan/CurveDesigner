using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.NewUI
{
    public class PositionHandleComposite : IComposite
    {
        private AxisHandleComposite rightAxis;
        private AxisHandleComposite upAxis;
        private AxisHandleComposite forwardAxis;
        private IPositionProvider positionProvider;
        public PositionHandleComposite(IComposite parent,Curve3D curve,IPositionProvider positionProvider) : base(parent)
        {
            this.positionProvider = positionProvider;
            rightAxis = new AxisHandleComposite(this, curve, Vector3.right,positionProvider);
            upAxis = new AxisHandleComposite(this, curve, Vector3.up,positionProvider);
            forwardAxis= new AxisHandleComposite(this, curve, Vector3.forward,positionProvider);
        }
        public override IEnumerable<IComposite> GetChildren()
        {
            yield return rightAxis;
            yield return upAxis;
            yield return forwardAxis;
        }
    }
}
