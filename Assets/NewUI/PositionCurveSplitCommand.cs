using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public abstract class SplitCommand : IClickCommand
    {
        protected Curve3D _curve;
        protected SplitterPointComposite _splitter;
        public SplitCommand(Curve3D curve,SplitterPointComposite splitter)
        {
            this._curve = curve;
            this._splitter = splitter;
        }
        public virtual void ClickDown(Vector2 mousePos) { }
        public virtual void ClickDrag(Vector2 mousePos, Curve3D curve, ClickHitData clicked) { }
        public virtual void ClickUp(Vector2 mousePos) { }
    }
    public class PositionCurveSplitCommand : SplitCommand
    {
        public PositionCurveSplitCommand(Curve3D curve, SplitterPointComposite splitter) : base(curve, splitter) { }
        public override void ClickDown(Vector2 mousePos)
        {
            _curve.positionCurve.InsertSegmentAfterIndex(_curve.UICurve.pointClosestToCursor,_curve.positionCurve.placeLockedPoints,_curve.positionCurve.splitInsertionBehaviour);
            _curve.UICurve.Initialize();//ideally we would only reinitialize the components that have updated. Basically we should be able to refresh the tree below any IComposite
        }
    }
    public class SizeCurveSplitCommand : SplitCommand
    {
        public SizeCurveSplitCommand(Curve3D curve, SplitterPointComposite splitter) : base(curve, splitter) { }
        public override void ClickDown(Vector2 mousePos)
        {
            _curve.sizeDistanceSampler.InsertPointAtDistance(_curve.UICurve.pointClosestToCursor.distanceFromStartOfCurve,_curve.isClosedLoop,_curve.positionCurve.GetLength(),_curve.curveRadius);
            _curve.UICurve.Initialize();//See above
        }
    }
    public abstract class Singleton<T> where T : class, new()
    {
        private static T _instance = null;
        public static T Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new T();
                return _instance;
            }
        }
    }
    public class PositionCurveSplitCommandFactory : Singleton<PositionCurveSplitCommandFactory>, ISplitCommandFactory
    {
        public IClickCommand Create(SplitterPointComposite owner, Curve3D curve)
        {
            return new PositionCurveSplitCommand(curve,owner);
        }
    }
    public class SizeCurveSplitCommandFactory : Singleton<SizeCurveSplitCommandFactory>, ISplitCommandFactory
    {
        public IClickCommand Create(SplitterPointComposite owner, Curve3D curve)
        {
            return new SizeCurveSplitCommand(curve, owner);
        }
    }
}
