using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class PositionPointGroupComposite : IComposite
    {
        private PointGroup _pointGroup;

        public PointComposite leftTangentPoint = null;
        private LineComposite leftTangentLine = null;

        public PointComposite rightTangentPoint = null;
        private LineComposite rightTangentLine = null;

        private PointComposite centerPoint = null;
        public PositionPointGroupComposite(IComposite parent, PointGroup group) : base(parent)
        {
            _pointGroup = group;
            var centerPointPosition = new PointGroupPointPositionProvider(_pointGroup, PGIndex.Position);
            centerPoint = new PointComposite(this,centerPointPosition,PointTextureType.circle,new PositionPointClickCommand(group,PGIndex.Position),Curve3DSettings.Green);
            if (_pointGroup.hasLeftTangent)
            {
                var endPoint = new PointGroupPointPositionProvider(_pointGroup, PGIndex.LeftTangent);
                leftTangentPoint = new PointComposite(this,endPoint,PointTextureType.square,new PositionPointClickCommand(group,PGIndex.LeftTangent),Curve3DSettings.Green);
                leftTangentLine = new LineComposite(this,centerPointPosition,endPoint);
            }
            if (_pointGroup.hasRightTangent)
            {
                var endPoint = new PointGroupPointPositionProvider(_pointGroup, PGIndex.RightTangent);
                rightTangentPoint = new PointComposite(this,endPoint,PointTextureType.square,new PositionPointClickCommand(group,PGIndex.RightTangent), Curve3DSettings.Green);
                rightTangentLine = new LineComposite(this,centerPointPosition, endPoint);
            }
        }

        public override IEnumerable<IComposite> GetChildren()
        {
            if (leftTangentPoint != null)
            {
                yield return leftTangentPoint;
                yield return leftTangentLine;
            }
            yield return centerPoint;
            if (rightTangentPoint != null)
            {
                yield return rightTangentPoint;
                yield return rightTangentLine;
            }
        }
    }
}
