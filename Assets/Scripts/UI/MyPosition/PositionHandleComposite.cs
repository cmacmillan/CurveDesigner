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
        private IPosition positionProvider;
        public PositionHandleComposite(IComposite parent,Curve3D curve,IPosition positionProvider,IOnPositionEdited onPositionEdited=null) : base(parent)
        {
            this.positionProvider = positionProvider;
            rightAxis = new AxisHandleComposite(this, curve, Vector3.right,positionProvider,onPositionEdited);
            upAxis = new AxisHandleComposite(this, curve, Vector3.up,positionProvider,onPositionEdited);
            forwardAxis= new AxisHandleComposite(this, curve, Vector3.forward,positionProvider,onPositionEdited);
        }
        public override IEnumerable<IComposite> GetChildren()
        {
            yield return rightAxis;
            yield return upAxis;
            yield return forwardAxis;
        }
    }
}
