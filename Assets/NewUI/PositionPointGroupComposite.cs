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
        private PointComposite rightTangentPoint = null;
        private PointComposite centerPoint = null;
        public PositionPointGroupComposite(PointGroup group)
        {
            _pointGroup = group;
            if (_pointGroup.hasLeftTangent)
                leftTangentPoint = new PointComposite(new PointGroupPointPositionProvider(_pointGroup,PGIndex.LeftTangent),PointTextureType.square,null);
            if (_pointGroup.hasRightTangent)
                rightTangentPoint = new PointComposite(new PointGroupPointPositionProvider(_pointGroup,PGIndex.RightTangent),PointTextureType.square,null);
            centerPoint = new PointComposite(new PointGroupPointPositionProvider(_pointGroup,PGIndex.Position),PointTextureType.circle,null);
        }

        public override ClickHitData Click(Vector2 position)
        {
            return null;
        }

        public override IEnumerable<IComposite> GetChildren()
        {
            if (leftTangentPoint != null)
                yield return leftTangentPoint;
            yield return centerPoint;
            if (rightTangentPoint != null)
                yield return rightTangentPoint;
        }
    }
}
