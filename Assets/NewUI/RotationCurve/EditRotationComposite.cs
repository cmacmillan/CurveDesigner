using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class EditRotationComposite : IComposite
    {
        private FloatDistanceValue _point;
        private BezierCurve _positionCurve;
        private PointComposite centerPoint;
        private PointComposite rotationHandlePoint;

        public EditRotationComposite(IComposite parent): base(parent)
        {
            //centerPoint = new PointComposite(this,,)
        }

    }
}
