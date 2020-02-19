using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class PositionCurveComposite : IComposite
    {
        private List<PositionPointGroupComposite> pointGroups = null;
        public PositionCurveComposite(BeizerCurve curve)
        {
            pointGroups = new List<PositionPointGroupComposite>();
            foreach (var group in curve.PointGroups)
                pointGroups.Add(new PositionPointGroupComposite(group));
        }

        public override IEnumerable<IComposite> GetChildren()
        {
            return pointGroups;
        }
    }
}
