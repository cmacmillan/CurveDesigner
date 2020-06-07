﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class PointAlongCurveComposite : IComposite, IPositionProvider
    {
        public IPointOnCurve value;
        public PointComposite point;
        private BezierCurve _positionCurve;

        public PointAlongCurveComposite(IComposite parent,IPointOnCurve value,PositionCurveComposite positionCurve,Color color) : base(parent)
        {
            this.value = value;
            point = new PointComposite(this, this, PointTextureType.square, new PointOnCurveClickCommand(value, positionCurve),color);
            _positionCurve = positionCurve.positionCurve;
        }

        public Vector3 Position {
            get
            {
                GetPositionForwardAndReference(out Vector3 position, out Vector3 forward,out Vector3 reference);
                return _positionCurve.owner.transform.TransformPoint(position);
            }
        }

        public void GetPositionForwardAndReference(out Vector3 position, out Vector3 forward, out Vector3 reference)
        {
            var point = _positionCurve.GetPointAtDistance(value.GetDistance(_positionCurve));
            position = point.position;
            forward = point.tangent;
            reference = point.reference;
        }

        public override IEnumerable<IComposite> GetChildren()
        {
            yield return point;
        }
    }
}
