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
            var selected = _curve.UICurve.positionCurve.pointGroups[_curve.UICurve.pointClosestToCursor.segmentIndex+1].centerPoint;
            _curve.elementClickedDown.owner = selected;
        }
    }
    public interface IValueAlongCurvePointProvider
    {
        IClickable GetPointAtIndex(int index);
    }
    public class ValueAlongCurveSplitCommand: SplitCommand
    {
        private FloatLinearDistanceSampler _sampler;
        private Func<Curve3D,IValueAlongCurvePointProvider> _pointsProvider;
        public ValueAlongCurveSplitCommand(Curve3D curve, SplitterPointComposite splitter,FloatLinearDistanceSampler sampler,Func<Curve3D,IValueAlongCurvePointProvider> pointsProvider) : base(curve, splitter) {
            _pointsProvider = pointsProvider;
            _sampler = sampler;
        }
        public override void ClickDown(Vector2 mousePos)
        {
            int index = _sampler.InsertPointAtDistance(_curve.UICurve.pointClosestToCursor.distanceFromStartOfCurve,_curve.isClosedLoop,_curve.positionCurve.GetLength());
            _curve.UICurve.Initialize();//See above
            var selected = _pointsProvider(_curve).GetPointAtIndex(index);
            _curve.elementClickedDown.owner = selected;
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
        private IValueAlongCurvePointProvider GetSizeCurve(Curve3D curve)
        {
            return curve.UICurve.sizeCurve;
        }
        public IClickCommand Create(SplitterPointComposite owner, Curve3D curve)
        {
            return new ValueAlongCurveSplitCommand(curve, owner,curve.sizeDistanceSampler,GetSizeCurve);
        }
    }
    public class RotationCurveSplitCommandFactory: Singleton<RotationCurveSplitCommandFactory>, ISplitCommandFactory
    {
        private IValueAlongCurvePointProvider GetRotationCurve(Curve3D curve)
        {
            return curve.UICurve.rotationCurve;
        }
        public IClickCommand Create(SplitterPointComposite owner, Curve3D curve)
        {
            return new ValueAlongCurveSplitCommand(curve, owner,curve.rotationDistanceSampler,GetRotationCurve);
        }
    }
}
