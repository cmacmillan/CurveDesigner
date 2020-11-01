using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class PositionCurveComposite : IComposite, IWindowDrawer
    {
        public List<PositionPointGroupComposite> pointGroups = null;
        private SplitterPointComposite _splitterPoint = null;
        private AddPositionPointButton _leftAddPositionPoint = null;
        private AddPositionPointButton _rightAddPositionPoint = null;
        private Curve3D curve3d;
        public BezierCurve positionCurve;
        public TransformBlob transformBlob;
        public PointOnCurve PointClosestToCursor { get; private set; }
        public PositionCurveComposite(IComposite parent,Curve3D curve,BezierCurve positionCurve,IClickCommand splitterPointClickCommand, TransformBlob transformBlob,List<BezierCurve> allCurves,int secondaryCurveIndex) : base(parent)
        {
            this.transformBlob = transformBlob;
            this.positionCurve = positionCurve;
            this.curve3d = curve;
            _splitterPoint = new SplitterPointComposite(this,transformBlob,PointTextureType.circle,splitterPointClickCommand,Curve3DSettings.Green,this);
            _leftAddPositionPoint = new AddPositionPointButton(this, curve, positionCurve, true,transformBlob,this,secondaryCurveIndex);
            _rightAddPositionPoint = new AddPositionPointButton(this, curve, positionCurve, false,transformBlob,this,secondaryCurveIndex);
            pointGroups = new List<PositionPointGroupComposite>();
            foreach (var group in positionCurve.PointGroups)
                pointGroups.Add(new PositionPointGroupComposite(this,group,transformBlob,positionCurve,group.GUID,allCurves,curve));
        }
        public void FindPointClosestToCursor()
        {
            List<PointOnCurve> samples = new List<PointOnCurve>();
            int numSamples = curve3d.samplesForCursorCollisionCheck;
            var length = positionCurve.GetLength();
            for (int i = 0; i < numSamples; i++)
                samples.Add(positionCurve.GetPointAtDistance(length * i / (numSamples - 1)));
            foreach (var i in samples)
                i.position = transformBlob.TransformPoint(i.position);
            int segmentIndex;
            float time;
            UnitySourceScripts.ClosestPointToPolyLine(out segmentIndex, out time, samples);
            foreach (var i in samples)
                i.position = transformBlob.InverseTransformPoint(i.position);
            float distance = positionCurve.GetDistanceAtSegmentIndexAndTime(segmentIndex,time);
            PointClosestToCursor = positionCurve.GetPointAtDistance(distance);
        }
        public override IEnumerable<IComposite> GetChildren()
        {
            yield return _splitterPoint;
            if (!positionCurve.isClosedLoop)
            {
                yield return _leftAddPositionPoint;
                yield return _rightAddPositionPoint;
            }
            foreach (var i in pointGroups)
                yield return i;
        }

        public void DrawWindow(Curve3D curve)
        {
            WindowDrawer.Draw(curve.positionCurve.PointGroups, curve);
        }
    }
}
