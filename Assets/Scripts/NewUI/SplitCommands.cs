﻿using System;
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

        public virtual void ClickDown(Vector2 mousePos,Curve3D curve, List<SelectableGUID> selectedPoints) { }
        public virtual void ClickDrag(Vector2 mousePos, Curve3D curve, ClickHitData clicked,List<SelectableGUID> selectedPoints) { }
        public virtual void ClickUp(Vector2 mousePos,Curve3D curve, List<SelectableGUID> selectedPoints) { }
    }
    public class DoubleBezierCurveSplitCommand : SplitCommand
    {
        private DoubleBezierSampler sampler;
        private PositionCurveComposite _positionCurveComposite;
        public DoubleBezierCurveSplitCommand(Curve3D curve, DoubleBezierSampler sampler,PositionCurveComposite positionCurveComposite) : base(curve)
        {
            _positionCurveComposite = positionCurveComposite;
            _curve = curve;
            this.sampler = sampler; 
        }
        public override void ClickDown(Vector2 mousePos,Curve3D curve, List<SelectableGUID> selectedPoints)
        {
            int index = sampler.InsertPointAtDistance(_positionCurveComposite.PointClosestToCursor.distanceFromStartOfCurve,_curve.isClosedLoop,_curve.positionCurve.GetLength(),_curve.positionCurve);
            _curve.UICurve.Initialize();
            var selected = _curve.UICurve.doubleBezierCurve.GetPointAtIndex(index);
            _curve.elementClickedDown.owner = selected;
        }
    }
    public class BackingCurveModificationTracker<T> where T : CurveTrackingValue
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
        private SecondaryPositionCurveComposite secondaryPositionCurveComposite;
        public SecondaryPositionCurveSplitCommand(BezierCurve secondaryPositionCurve,Curve3D curve,SecondaryPositionCurveComposite secondaryPositionCurveComposite)
        {
            this.secondaryPositionCurveComposite = secondaryPositionCurveComposite;
            this.secondaryPositionCurve = secondaryPositionCurve;
            this.curve = curve;
        }

        public void ClickDown(Vector2 mousePos,Curve3D curve, List<SelectableGUID> selectedPoints)
        {
            var closestPoint = secondaryPositionCurveComposite.positionCurve.PointClosestToCursor;
            secondaryPositionCurve.InsertSegmentAfterIndex(closestPoint,curve.placeLockedPoints,curve.splitInsertionBehaviour);
            curve.UICurve.Initialize();
            var newSecondaryCurve = curve.UICurve.doubleBezierCurve.GetSecondaryCompositeByBackingCurve(secondaryPositionCurve);
            var selected = newSecondaryCurve.positionCurve.pointGroups[closestPoint.segmentIndex + 1].centerPoint;
            curve.elementClickedDown.owner = selected;
        }

        public void ClickDrag(Vector2 mousePos, Curve3D curve, ClickHitData clicked, List<SelectableGUID> selectedPoints) { }

        public void ClickUp(Vector2 mousePos,Curve3D curve, List<SelectableGUID> selectedPoints) { }
    }
    public class MainPositionCurveSplitCommand : SplitCommand
    {
        public MainPositionCurveSplitCommand(Curve3D curve) : base(curve) { }

        public override void ClickDown(Vector2 mousePos,Curve3D curve, List<SelectableGUID> selectedPoints)
        {
            List<BackingCurveModificationTracker<FloatDistanceValue>> distanceSamplerModificationTrackers = new List<BackingCurveModificationTracker<FloatDistanceValue>>();
            foreach (var i in _curve.DistanceSamplers)
                distanceSamplerModificationTrackers.Add(new BackingCurveModificationTracker<FloatDistanceValue>(_curve.positionCurve,i._points));
            var doubleBezier = _curve.doubleBezierSampler;
            var doubleBezierModificationTracker = new BackingCurveModificationTracker<BezierCurveDistanceValue>(_curve.positionCurve,doubleBezier.secondaryCurves);
            var closestPoint = _curve.UICurve.positionCurve.PointClosestToCursor;
            _curve.positionCurve.InsertSegmentAfterIndex(closestPoint,_curve.placeLockedPoints,_curve.splitInsertionBehaviour);
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
        private Func<Curve3D,IValueAlongCurvePointProvider> _pointsProvider;

        public static IValueAlongCurvePointProvider GetRotationCurve(Curve3D curve) { return curve.UICurve.rotationCurve; }
        public static IValueAlongCurvePointProvider GetSizeCurve(Curve3D curve) { return curve.UICurve.sizeCurve; }
        public static IValueAlongCurvePointProvider GetDoubleBezierCurve(Curve3D curve) { return curve.UICurve.doubleBezierCurve; }

        public ValueAlongCurveSplitCommand(Curve3D curve, FloatLinearDistanceSampler sampler,Func<Curve3D,IValueAlongCurvePointProvider> pointsProvider) : base(curve) {
            _pointsProvider = pointsProvider;
            _sampler = sampler;
            _curve = curve;
        }
        public override void ClickDown(Vector2 mousePos,Curve3D curve, List<SelectableGUID> selectedPoints)
        {
            int index = _sampler.InsertPointAtDistance(_curve.UICurve.positionCurve.PointClosestToCursor.distanceFromStartOfCurve,_curve.isClosedLoop,_curve.positionCurve.GetLength(),_curve.positionCurve);
            _curve.UICurve.Initialize();//See above
            var selected = _pointsProvider(_curve).GetPointAtIndex(index);
            _curve.elementClickedDown.owner = selected;
        }
    }
}
