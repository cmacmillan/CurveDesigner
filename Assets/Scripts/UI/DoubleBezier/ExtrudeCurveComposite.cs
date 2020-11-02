﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class ExtrudeCurveComposite : IComposite, IValueAlongCurvePointProvider, IWindowDrawer
    {
        private ExtrudeSampler _extrudeSampler;
        public List<SecondaryPositionCurveComposite> _secondaryCurves;
        private SplitterPointComposite _splitterPoint;
        public ExtrudeCurveComposite(IComposite parent,ExtrudeSampler extrudeSampler,Curve3D curve,PositionCurveComposite positionCurveComposite) : base(parent)
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
        public IClickable GetPointAtIndex(int index)
        {
            return _secondaryCurves[index].centerPoint.point;
        }
        public override IEnumerable<IComposite> GetChildren()
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