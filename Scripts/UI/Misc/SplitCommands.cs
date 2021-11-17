#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
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
    public class ModificationTracker
    {
        private struct CurveItem
        {
            public CurveItem(float distance, bool isTracked)
            {
                this.distance = distance;
                this.isTracked = isTracked;
            }
            public float distance;
            public bool isTracked;
        }
        private List<CurveItem> modifications = new List<CurveItem>();
        private List<ISamplerPoint> points = new List<ISamplerPoint>();
        private BezierCurve curve;
        public ModificationTracker(BezierCurve curve, ISampler distanceSampler)
        {
            this.curve = curve;
            foreach (var i in distanceSampler.AllPoints())
            {
                points.Add(i);
                if (i.SegmentIndex < curve.NumSegments)
                    modifications.Add(new CurveItem(i.GetDistance(curve),true));
                else
                    modifications.Add(new CurveItem(-1,false));
            }
        }
        public void FinishInsertToBackingCurve()
        {
            for (int i = 0; i < points.Count; i++)
            {
                var curr = modifications[i];
                if (curr.isTracked)
                    points[i].SetDistance(curr.distance,curve,false);
            }
        }
    }
    public class AddPositionPointClickCommand : IClickCommand
    {
        private bool isPrepend;
        private BezierCurve curveToModify;
        private AddPositionPointButton button;
        private bool isMainPositionCurve => secondaryCurveIndex == -1;
        private int secondaryCurveIndex;
        private TransformBlob transformBlob;
        public AddPositionPointClickCommand(bool isPrepend,BezierCurve curveToModify,AddPositionPointButton button, int secondaryCurveIndex,TransformBlob transformBlob)
        {
            this.curveToModify = curveToModify;
            this.isPrepend = isPrepend;
            this.button = button;
            this.secondaryCurveIndex = secondaryCurveIndex;
            this.transformBlob = transformBlob;
        }
        public void ClickDown(Vector2 mousePos, Curve3D curve, List<SelectableGUID> selectedPoints)
        {
            var pointGuid = curveToModify.AppendPoint(isPrepend,curve.placeLockedPoints,transformBlob.InverseTransformPoint(button.Position));
            int bumpAboveIndex = isPrepend ? 0 : curveToModify.PointGroups.Count-2;
            if (isMainPositionCurve)//secondary position curves don't have samplers that need to be bumped
            {
                foreach (var sampler in curve.DistanceSamplers)
                    foreach (var point in sampler.AllPoints())
                        if (point.SegmentIndex >= bumpAboveIndex)
                            point.SegmentIndex++;
            }

            curve.SelectOnlyPoint(pointGuid);
            curve.UICurve.Initialize();

            int finalIndex = isPrepend ? 0 : curveToModify.PointGroups.Count-1;
            PositionCurveComposite posCurve;
            if (isMainPositionCurve)
                posCurve = curve.UICurve.positionCurve;
            else
                posCurve = curve.UICurve.extrudeCurve._secondaryCurves[secondaryCurveIndex].positionCurve;
            var selected = posCurve.pointGroups[finalIndex].centerPoint;
            curve.elementClickedDown.owner = selected;
        }

        public void ClickDrag(Vector2 mousePos, Curve3D curve, ClickHitData clicked, List<SelectableGUID> selected){ }

        public void ClickUp(Vector2 mousePos, Curve3D curve, List<SelectableGUID> selected) { }
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
            var newSecondaryCurve = curve.UICurve.extrudeCurve.GetSecondaryCompositeByBackingCurve(secondaryPositionCurve);
            var selected = newSecondaryCurve.positionCurve.pointGroups[closestPoint.segmentIndex + 1].centerPoint;
            curve.SelectOnlyPoint(selected.GUID);
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
            /////////////////
            var modificationTrackers = new List<ModificationTracker>();
            foreach (var i in _curve.DistanceSamplers)
                modificationTrackers.Add(new ModificationTracker(curve.positionCurve, i));
            /////////////////

            var closestPoint = _curve.UICurve.positionCurve.PointClosestToCursor;
            var pointGuid = _curve.positionCurve.InsertSegmentAfterIndex(closestPoint,_curve.placeLockedPoints,_curve.splitInsertionBehaviour);
            _curve.SelectOnlyPoint(pointGuid);
            _curve.UICurve.Initialize();//ideally we would only reinitialize the components that have updated. Basically we should be able to refresh the tree below any IComposite

            /////////////////
            foreach (var i in modificationTrackers)
                i.FinishInsertToBackingCurve();
            ////////////////

            var selected = _curve.UICurve.positionCurve.pointGroups[closestPoint.segmentIndex+1].centerPoint;
            _curve.elementClickedDown.owner = selected;
        }
    }
    public class ValueAlongCurveSplitCommand: SplitCommand
    {
        private ISampler _sampler;
        private Func<Curve3D,IValueAlongCurvePointProvider> _pointsProvider;

        public static IValueAlongCurvePointProvider GetRotationCurve(Curve3D curve) { return curve.UICurve.rotationCurve; }
        public static IValueAlongCurvePointProvider GetSizeCurve(Curve3D curve) { return curve.UICurve.sizeCurve; }
        public static IValueAlongCurvePointProvider GetThicknessCurve(Curve3D curve) { return curve.UICurve.thicknessCurve; }
        public static IValueAlongCurvePointProvider GetArcCurve(Curve3D curve) { return curve.UICurve.arcCurve; }
        public static IValueAlongCurvePointProvider GetColorCurve(Curve3D curve) { return curve.UICurve.colorCurve; }
        public static IValueAlongCurvePointProvider GetExtrudeCurve(Curve3D curve) { return curve.UICurve.extrudeCurve; }

        public ValueAlongCurveSplitCommand(Curve3D curve, ISampler sampler,Func<Curve3D,IValueAlongCurvePointProvider> pointsProvider) : base(curve) {
            _pointsProvider = pointsProvider;
            _sampler = sampler;
            _curve = curve;
        }
        public override void ClickDown(Vector2 mousePos,Curve3D curve, List<SelectableGUID> selectedPoints)
        {
            int index = _sampler.InsertPointAtDistance(_curve.UICurve.positionCurve.PointClosestToCursor.distanceFromStartOfCurve, _curve.positionCurve);
            _curve.UICurve.Initialize();//See above
            var selected = _pointsProvider(_curve).GetPointAtIndex(index);
            curve.SelectOnlyPoint(selected.GUID);
            _curve.elementClickedDown.owner = selected;
        }
    }
}
#endif
