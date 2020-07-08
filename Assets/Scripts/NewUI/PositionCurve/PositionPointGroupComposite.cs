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
        private BezierCurve _positionCurve;

        private TransformBlob _transformBlob;

        public override SelectableGUID GUID => _pointGroup.GUID;

        public IClickCommand GetCenterPointClickCommand()
        {
            return new PositionPointClickCommand(_pointGroup, PGIndex.Position,_positionCurve,_transformBlob);
        }
        public PositionPointGroupComposite(IComposite parent, PointGroup group, TransformBlob transformBlob, BezierCurve positionCurve,SelectableGUID guid) : base(parent)
        {
            this._transformBlob = transformBlob;
            _pointGroup = group;
            this._positionCurve = positionCurve;
            var centerPointPosition = new PointGroupPointPositionProvider(_pointGroup, PGIndex.Position,transformBlob,_positionCurve);
            centerPoint = new PointComposite(this,centerPointPosition,PointTextureType.circle,GetCenterPointClickCommand(),Curve3DSettings.Green,guid);
            bool isCurveClosedLoop = positionCurve.isClosedLoop;
            bool isStartPoint = group == positionCurve.PointGroups[0];
            bool isEndPoint = group == positionCurve.PointGroups[positionCurve.PointGroups.Count-1];
            //left tangent
            if (!isStartPoint || isCurveClosedLoop)
            {
                var endPoint = new PointGroupPointPositionProvider(_pointGroup, PGIndex.LeftTangent,transformBlob,_positionCurve);
                leftTangentPoint = new PointComposite(this,endPoint,PointTextureType.square,new PositionPointClickCommand(group,PGIndex.LeftTangent,_positionCurve,_transformBlob),Curve3DSettings.Green,guid);
                leftTangentLine = new LineComposite(this,centerPointPosition,endPoint);
            }
            //right tangent
            if (!isEndPoint || isCurveClosedLoop)
            {
                var endPoint = new PointGroupPointPositionProvider(_pointGroup, PGIndex.RightTangent,transformBlob,_positionCurve);
                rightTangentPoint = new PointComposite(this,endPoint,PointTextureType.square,new PositionPointClickCommand(group,PGIndex.RightTangent,_positionCurve,_transformBlob), Curve3DSettings.Green,guid);
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
