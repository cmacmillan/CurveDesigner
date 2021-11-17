#if UNITY_EDITOR
using System.Collections.Generic;

namespace ChaseMacMillan.CurveDesigner
{
    public class PositionPointGroupComposite : Composite
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
            return new PositionPointClickCommand(_pointGroup, PointGroupIndex.Position,_positionCurve,_transformBlob,allCurves);
        }
        public PositionPointGroupComposite(Composite parent, PointGroup group, TransformBlob transformBlob, BezierCurve positionCurve,SelectableGUID guid,List<BezierCurve> allCurves,Curve3D curve) : base(parent)
        {
            this._transformBlob = transformBlob;
            this.curve = curve;
            _pointGroup = group;
            this._positionCurve = positionCurve;
            this.allCurves = allCurves;
            var centerPointPosition = new PointGroupPointPositionProvider(_pointGroup, PointGroupIndex.Position,transformBlob,_positionCurve,curve);
            centerPoint = new PointComposite(this,centerPointPosition,PointTextureType.circle,GetCenterPointClickCommand(),CurveUIStatic.Green,guid);
            centerPositionHandle = new PositionHandleComposite(this, curve, centerPointPosition);
            bool isCurveClosedLoop = positionCurve.isClosedLoop;
            bool isStartPoint = group == positionCurve.PointGroups[0];
            bool isEndPoint = group == positionCurve.PointGroups[positionCurve.PointGroups.Count-1];
            //left tangent
            if (!isStartPoint || isCurveClosedLoop)
            {
                var endPoint = new PointGroupPointPositionProvider(_pointGroup, PointGroupIndex.LeftTangent,transformBlob,_positionCurve,curve);
                leftTangentPoint = new PointComposite(this,endPoint,PointTextureType.square,new PositionPointClickCommand(group,PointGroupIndex.LeftTangent,_positionCurve,_transformBlob,allCurves),CurveUIStatic.Green,guid);
                leftTangentLine = new LineComposite(this,centerPointPosition,endPoint);
                leftTangentPositionHandle= new PositionHandleComposite(this, curve, endPoint);
            }
            //right tangent
            if (!isEndPoint || isCurveClosedLoop)
            {
                var endPoint = new PointGroupPointPositionProvider(_pointGroup, PointGroupIndex.RightTangent,transformBlob,_positionCurve,curve);
                rightTangentPoint = new PointComposite(this,endPoint,PointTextureType.square,new PositionPointClickCommand(group,PointGroupIndex.RightTangent,_positionCurve,_transformBlob,allCurves), CurveUIStatic.Green,guid);
                rightTangentLine = new LineComposite(this,centerPointPosition, endPoint);
                rightTangentPositionHandle= new PositionHandleComposite(this, curve, endPoint);
            }
        }

        public override IEnumerable<Composite> GetChildren()
        {
            bool isSelected = curve.selectedPoints.Contains(GUID)&&curve.showPositionHandles;
            if (leftTangentPoint != null && !_positionCurve.automaticTangents)
            {
                yield return leftTangentPoint;
                yield return leftTangentLine;
                if (isSelected)
                    yield return leftTangentPositionHandle;
            }
            yield return centerPoint;
            if (isSelected)
                yield return centerPositionHandle;
            if (rightTangentPoint != null && !_positionCurve.automaticTangents)
            {
                yield return rightTangentPoint;
                yield return rightTangentLine;
                if (isSelected)
                    yield return rightTangentPositionHandle;
            }
        }
    }
}
#endif
