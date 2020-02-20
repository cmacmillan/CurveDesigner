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

        private PointComposite leftTangentPoint = null;
        private LineComposite leftTangentLine = null;

        private PointComposite rightTangentPoint = null;
        private LineComposite rightTangentLine = null;

        private PointComposite centerPoint = null;
        public PositionPointGroupComposite(PointGroup group)
        {
            _pointGroup = group;
            var centerPointPosition = new PointGroupPointPositionProvider(_pointGroup, PGIndex.Position);
            centerPoint = new PointComposite(centerPointPosition,PointTextureType.circle,new PositionPointClickCommand(group,PGIndex.Position),Color.green);
            if (_pointGroup.hasLeftTangent)
            {
                var endPoint = new PointGroupPointPositionProvider(_pointGroup, PGIndex.LeftTangent);
                leftTangentPoint = new PointComposite(endPoint,PointTextureType.square,new PositionPointClickCommand(group,PGIndex.LeftTangent),Color.green);
                leftTangentLine = new LineComposite(centerPointPosition,endPoint);
            }
            if (_pointGroup.hasRightTangent)
            {
                var endPoint = new PointGroupPointPositionProvider(_pointGroup, PGIndex.RightTangent);
                rightTangentPoint = new PointComposite(endPoint,PointTextureType.square,new PositionPointClickCommand(group,PGIndex.RightTangent),Color.green);
                rightTangentLine = new LineComposite(centerPointPosition, endPoint);
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
