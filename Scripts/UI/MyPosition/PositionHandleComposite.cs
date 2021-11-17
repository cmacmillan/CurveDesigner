#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public class PositionHandleComposite : Composite
    {
        private AxisHandleComposite rightAxis;
        private AxisHandleComposite upAxis;
        private AxisHandleComposite forwardAxis;
        private IPosition positionProvider;
        public PositionHandleComposite(Composite parent,Curve3D curve,IPosition positionProvider) : base(parent)
        {
            this.positionProvider = positionProvider;
            rightAxis = new AxisHandleComposite(this, curve, Vector3.right,positionProvider);
            upAxis = new AxisHandleComposite(this, curve, Vector3.up,positionProvider);
            forwardAxis= new AxisHandleComposite(this, curve, Vector3.forward,positionProvider);
        }
        public override IEnumerable<Composite> GetChildren()
        {
            yield return rightAxis;
            yield return upAxis;
            yield return forwardAxis;
        }
    }
}
#endif
