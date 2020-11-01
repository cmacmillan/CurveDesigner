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
        private PositionHandleComposite leftTangentPositionHandle = null;

        public PointComposite rightTangentPoint = null;
        private LineComposite rightTangentLine = null;
        private PositionHandleComposite rightTangentPositionHandle = null;

        public PointComposite centerPoint = null;
        private BezierCurve _positionCurve;
        private PositionHandleComposite centerPositionHandle = null;

        private TransformBlob _transformBlob;

        private Curve3D curve;

        public override SelectableGUID GUID => _pointGroup.GUID;

        private List<BezierCurve> allCurves;

        public IClickCommand GetCenterPointClickCommand()
        {
            return new PositionPointClickCommand(_pointGroup, PGIndex.Position,_positionCurve,_transformBlob,allCurves);
        }
        public PositionPointGroupComposite(IComposite parent, PointGroup group, TransformBlob transformBlob, BezierCurve positionCurve,SelectableGUID guid,List<BezierCurve> allCurves,Curve3D curve) : base(parent)
        {
            this._transformBlob = transformBlob;
            this.curve = curve;
            _pointGroup = group;
            this._positionCurve = positionCurve;
            this.allCurves = allCurves;
            var centerPointPosition = new PointGroupPointPositionProvider(_pointGroup, PGIndex.Position,transformBlob,_positionCurve);
            centerPoint = new PointComposite(this,centerPointPosition,PointTextureType.circle,GetCenterPointClickCommand(),Curve3DSettings.Green,guid);
            centerPositionHandle = new PositionHandleComposite(this, curve, centerPointPosition,positionCurve);
            bool isCurveClosedLoop = positionCurve.isClosedLoop;
            bool isStartPoint = group == positionCurve.PointGroups[0];
            bool isEndPoint = group == positionCurve.PointGroups[positionCurve.PointGroups.Count-1];
            //left tangent
            if (!isStartPoint || isCurveClosedLoop)
            {
                var endPoint = new PointGroupPointPositionProvider(_pointGroup, PGIndex.LeftTangent,transformBlob,_positionCurve);
                leftTangentPoint = new PointComposite(this,endPoint,PointTextureType.square,new PositionPointClickCommand(group,PGIndex.LeftTangent,_positionCurve,_transformBlob,allCurves),Curve3DSettings.Green,guid);
                leftTangentLine = new LineComposite(this,centerPointPosition,endPoint);
                leftTangentPositionHandle= new PositionHandleComposite(this, curve, endPoint,positionCurve);
            }
            //right tangent
            if (!isEndPoint || isCurveClosedLoop)
            {
                var endPoint = new PointGroupPointPositionProvider(_pointGroup, PGIndex.RightTangent,transformBlob,_positionCurve);
                rightTangentPoint = new PointComposite(this,endPoint,PointTextureType.square,new PositionPointClickCommand(group,PGIndex.RightTangent,_positionCurve,_transformBlob,allCurves), Curve3DSettings.Green,guid);
                rightTangentLine = new LineComposite(this,centerPointPosition, endPoint);
                rightTangentPositionHandle= new PositionHandleComposite(this, curve, endPoint,positionCurve);
            }
        }

        public override IEnumerable<IComposite> GetChildren()
        {
            bool isSelected = curve.selectedPoints.Contains(GUID)&&curve.showPositionHandles;
            if (leftTangentPoint != null)
            {
                yield return leftTangentPoint;
                yield return leftTangentLine;
                if (isSelected)
                    yield return leftTangentPositionHandle;
            }
            yield return centerPoint;
            if (isSelected)
                yield return centerPositionHandle;
            if (rightTangentPoint != null)
            {
                yield return rightTangentPoint;
                yield return rightTangentLine;
                if (isSelected)
                    yield return rightTangentPositionHandle;
            }
        }
    }
}
