using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    class SizeCurveComposite : IComposite
    {
        private AnimationCurve _curve;
        public SizeCurveComposite(IComposite parent,AnimationCurve animCurve) : base(parent)
        {
            _curve = animCurve;
        }
    }
}
