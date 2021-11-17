#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public class ExtrudeCurveComposite : Composite, IValueAlongCurvePointProvider, IWindowDrawer
    {
        private ExtrudeSampler _extrudeSampler;
        public List<SecondaryPositionCurveComposite> _secondaryCurves;
        private SplitterPointComposite _splitterPoint;
        public ExtrudeCurveComposite(Composite parent,ExtrudeSampler extrudeSampler,Curve3D curve,PositionCurveComposite positionCurveComposite) : base(parent)
        {
            _extrudeSampler = extrudeSampler;
            _secondaryCurves = new List<SecondaryPositionCurveComposite>();
            _splitterPoint = new SplitterPointComposite(this,positionCurveComposite.transformBlob, PointTextureType.circle, new ValueAlongCurveSplitCommand(curve,_extrudeSampler,ValueAlongCurveSplitCommand.GetExtrudeCurve), Color.green,positionCurveComposite);
            var allCurves = new List<BezierCurve>();
            var points = extrudeSampler.GetPoints(curve.positionCurve);
            foreach (var i in points)
                allCurves.Add(i.value);
            int curveIndex = 0;
            foreach (var i in extrudeSampler.GetPoints(curve.positionCurve))
            {
                _secondaryCurves.Add(new SecondaryPositionCurveComposite(this,curve,i,extrudeSampler,allCurves,curveIndex));
                curveIndex++;
            }
        }
        public void FindClosestPointsToCursor()
        {
            foreach (var i in _secondaryCurves)
                i.positionCurve.FindPointClosestToCursor();
        }
        public SecondaryPositionCurveComposite GetSecondaryCompositeByBackingCurve(BezierCurve backingCurve)
        {
            foreach (var i in _secondaryCurves)
                if (i.positionCurve.positionCurve == backingCurve)
                    return i;
            throw new KeyNotFoundException();
        }
        public Clickable GetPointAtIndex(int index)
        {
            return _secondaryCurves[index].centerPoint.point;
        }
        public override IEnumerable<Composite> GetChildren()
        {
            yield return _splitterPoint;
            foreach (var i in _secondaryCurves)
                yield return i;
        }

        public void DrawWindow(Curve3D curve)
        {
            WindowDrawer.Draw(_extrudeSampler.GetPoints(curve.positionCurve),curve);
        }
    }
}
#endif
