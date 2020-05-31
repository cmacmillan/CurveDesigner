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

        public PointComposite centerPoint = null;
        public IClickCommand GetCenterPointClickCommand()
        {
            return new PositionPointClickCommand(_pointGroup, PGIndex.Position);
        }
        public PositionPointGroupComposite(IComposite parent, PointGroup group, Transform baseCurveTransform, BezierCurve positionCurve) : base(parent)
        {
            _pointGroup = group;
            var centerPointPosition = new PointGroupPointPositionProvider(_pointGroup, PGIndex.Position,baseCurveTransform);
            centerPoint = new PointComposite(this,centerPointPosition,PointTextureType.circle,GetCenterPointClickCommand(),Curve3DSettings.Green);
            bool isCurveClosedLoop = positionCurve.isClosedLoop;
            bool isStartPoint = group == positionCurve.PointGroups[0];
            bool isEndPoint = group == positionCurve.PointGroups[positionCurve.PointGroups.Count-1];
            //left tangent
            if (!isStartPoint || isCurveClosedLoop)
            {
                var endPoint = new PointGroupPointPositionProvider(_pointGroup, PGIndex.LeftTangent,baseCurveTransform);
                leftTangentPoint = new PointComposite(this,endPoint,PointTextureType.square,new PositionPointClickCommand(group,PGIndex.LeftTangent),Curve3DSettings.Green);
                leftTangentLine = new LineComposite(this,centerPointPosition,endPoint);
            }
            //right tangent
            if (!isEndPoint || isCurveClosedLoop)
            {
                var endPoint = new PointGroupPointPositionProvider(_pointGroup, PGIndex.RightTangent,baseCurveTransform);
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
