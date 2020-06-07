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
        public SplitCommand(Curve3D curve)
        {
            this._curve = curve;
        }
        public virtual void ClickDown(Vector2 mousePos) { }
        public virtual void ClickDrag(Vector2 mousePos, Curve3D curve, ClickHitData clicked) { }
        public virtual void ClickUp(Vector2 mousePos) { }
    }
    public class DoubleBezierCurveSplitCommand : SplitCommand
    {
        private DoubleBezierSampler sampler;
        private Curve3D _curve;
        public DoubleBezierCurveSplitCommand(Curve3D curve, DoubleBezierSampler sampler) : base(curve)
        {
            _curve = curve;
            this.sampler = sampler; 
        }
        public override void ClickDown(Vector2 mousePos)
        {
            int index = sampler.InsertPointAtDistance(_curve.UICurve.positionCurve.PointClosestToCursor.distanceFromStartOfCurve,_curve.isClosedLoop,_curve.positionCurve.GetLength(),_curve.positionCurve);
            _curve.UICurve.Initialize();
            var selected = _curve.UICurve.doubleBezierCurve.GetPointAtIndex(index);
            _curve.elementClickedDown.owner = selected;
        }
    }
    public class BackingCurveModificationTracker<T> where T : CurveTrackingDistance
    {
        private struct BackingCurveItem
        {
            public BackingCurveItem(float distance, bool isTracked)
            {
                this.distance = distance;
                this.isTracked = isTracked;
            }
            public float distance;
            public bool isTracked;
        }
        private BezierCurve backingCurve;
        private List<T> points;
        List<BackingCurveItem> backingCurveModifications;
        public BackingCurveModificationTracker(BezierCurve backingCurve,List<T> points)
        {
            this.points = points;
            this.backingCurve = backingCurve;
            backingCurveModifications = new List<BackingCurveItem>();
            foreach (var i in points)
            {
                if (i.SegmentIndex < this.backingCurve.NumSegments)
                    backingCurveModifications.Add(new BackingCurveItem(i.GetDistance(backingCurve),true));
                else
                    backingCurveModifications.Add(new BackingCurveItem(-1,false));
            }
        }
        public void FinishInsertToBackingCurve()
        {
            for (int i = 0; i < points.Count; i++)
            {
                var curr = backingCurveModifications[i];
                if (curr.isTracked)
                    points[i].SetDistance(curr.distance,backingCurve,false);
            }
            backingCurveModifications = null;
        }
    }
    public class SecondaryPositionCurveSplitCommand : IClickCommand
    {
        private Curve3D curve;
        private BezierCurve secondaryPositionCurve;
        public SecondaryPositionCurveSplitCommand(BezierCurve secondaryPositionCurve,Curve3D curve)
        {
            this.secondaryPositionCurve = secondaryPositionCurve;
            this.curve = curve;
        }
        public void ClickDown(Vector2 mousePos)
        {
            curve.UICurve.Initialize();
            Debug.Log("not implemented");
        }

        public void ClickDrag(Vector2 mousePos, Curve3D curve, ClickHitData clicked) { }

        public void ClickUp(Vector2 mousePos) { }
    }
    public class MainPositionCurveSplitCommand : SplitCommand
    {
        public MainPositionCurveSplitCommand(Curve3D curve) : base(curve) { }
        public override void ClickDown(Vector2 mousePos)
        {
            List<BackingCurveModificationTracker<FloatDistanceValue>> distanceSamplerModificationTrackers = new List<BackingCurveModificationTracker<FloatDistanceValue>>();
            foreach (var i in _curve.DistanceSamplers)
                distanceSamplerModificationTrackers.Add(new BackingCurveModificationTracker<FloatDistanceValue>(_curve.positionCurve,i._points));
            var doubleBezier = _curve.doubleBezierSampler;
            var doubleBezierModificationTracker = new BackingCurveModificationTracker<BezierCurveDistanceValue>(_curve.positionCurve,doubleBezier.secondaryCurves);
            var closestPoint = _curve.UICurve.positionCurve.PointClosestToCursor;
            _curve.positionCurve.InsertSegmentAfterIndex(closestPoint,_curve.positionCurve.placeLockedPoints,_curve.positionCurve.splitInsertionBehaviour);
            _curve.UICurve.Initialize();//ideally we would only reinitialize the components that have updated. Basically we should be able to refresh the tree below any IComposite
            foreach (var i in distanceSamplerModificationTrackers)
                i.FinishInsertToBackingCurve();
            doubleBezierModificationTracker.FinishInsertToBackingCurve();
            var selected = _curve.UICurve.positionCurve.pointGroups[closestPoint.segmentIndex+1].centerPoint;
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
        private IValueAlongCurvePointProvider _pointsProvider;
        public ValueAlongCurveSplitCommand(Curve3D curve, FloatLinearDistanceSampler sampler,IValueAlongCurvePointProvider pointsProvider) : base(curve) {
            _pointsProvider = pointsProvider;
            _sampler = sampler;
        }
        public override void ClickDown(Vector2 mousePos)
        {
            int index = _sampler.InsertPointAtDistance(_curve.UICurve.positionCurve.PointClosestToCursor.distanceFromStartOfCurve,_curve.isClosedLoop,_curve.positionCurve.GetLength(),_curve.positionCurve);
            _curve.UICurve.Initialize();//See above
            var selected = _pointsProvider.GetPointAtIndex(index);
            _curve.elementClickedDown.owner = selected;
        }
    }
}
