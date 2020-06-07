using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class PositionCurveComposite : IComposite
    {
        public List<PositionPointGroupComposite> pointGroups = null;
        private SplitterPointComposite _splitterPoint = null;
        public BezierCurve positionCurve;
        private TransformBlob transformBlob;
        public PointOnCurve PointClosestToCursor { get; private set; }
        public PositionCurveComposite(IComposite parent,Curve3D curve,BezierCurve positionCurve,IClickCommand clickCommand, TransformBlob transformBlob) : base(parent)
        {
            this.transformBlob = transformBlob;
            this.positionCurve = positionCurve;
            _splitterPoint = new SplitterPointComposite(this,transformBlob,PointTextureType.circle,clickCommand,Curve3DSettings.Green,this);
            pointGroups = new List<PositionPointGroupComposite>();
            foreach (var group in positionCurve.PointGroups)
                pointGroups.Add(new PositionPointGroupComposite(this,group,transformBlob,positionCurve));
        }
        public void FindPointClosestToCursor()
        {
            var samples = positionCurve.GetSamplePoints();
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
            foreach (var i in pointGroups)
                yield return i;
        }
        public override void Draw(List<IDraw> drawList, ClickHitData clickedElement)
        {
            FindPointClosestToCursor();
            base.Draw(drawList, clickedElement);
        }
        public override void Click(Vector2 mousePosition, List<ClickHitData> clickHits)
        {
            FindPointClosestToCursor();
            base.Click(mousePosition, clickHits);
        }
    }
}
