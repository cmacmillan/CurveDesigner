using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class RotationCurveComposite : IComposite 
    {
        private FloatLinearDistanceSampler _distanceSampler;
        private List<EditRotationComposite> _points = new List<EditRotationComposite>();
        public RotationCurveComposite(IComposite parent,FloatLinearDistanceSampler distanceSampler,Curve3D curve) : base(parent)
        {
            _distanceSampler = distanceSampler;
            var blueColor = new Color(0,.8f,1.0f);
            foreach (var i in distanceSampler.GetPoints(curve))
                _points.Add(new EditRotationComposite(this));
        }
        public override IEnumerable<IComposite> GetChildren()
        {
            foreach (var i in _points)
                yield return i;
        }
    }
}
