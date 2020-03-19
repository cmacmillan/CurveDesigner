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
        private SplitterPointComposite _splitterPoint = null;
        public PositionCurveComposite(IComposite parent,Curve3D curve) : base(parent)
        {
            _splitterPoint = new SplitterPointComposite(this,curve,PointTextureType.circle,PositionCurveSplitCommandFactory.Instance,Curve3DSettings.Green);
            pointGroups = new List<PositionPointGroupComposite>();
            foreach (var group in curve.positionCurve.PointGroups)
                pointGroups.Add(new PositionPointGroupComposite(this,group,curve));
        }

        public override IEnumerable<IComposite> GetChildren()
        {
            yield return _splitterPoint;
            foreach (var i in pointGroups)
                yield return i;
        }
    }
}
