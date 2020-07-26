using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class SplitterPointComposite : IComposite, IPositionProvider
    {
        private PointComposite _point;
        private TransformBlob _transformBlob;
        private const float _maxSplitClickDistance = 10;
        private PositionCurveComposite _positionCurveComposite;
        public SplitterPointComposite(IComposite parent,TransformBlob transformBlob,PointTextureType textureType,IClickCommand clickCommand,Color color,PositionCurveComposite positionCurveComposite) : base (parent)
        {
            _positionCurveComposite = positionCurveComposite;
            _transformBlob = transformBlob;
            _point = new PointComposite(this,this,textureType,clickCommand,color,SelectableGUID.Null);
        }
        public override void Draw(List<IDraw> drawlist,ClickHitData closestElementToCursor)
        {
            if (closestElementToCursor != null && closestElementToCursor.owner != null && closestElementToCursor.owner.GetParent() == this && closestElementToCursor.distanceFromClick < _maxSplitClickDistance)
                base.Draw(drawlist,closestElementToCursor);
        }
        public override IEnumerable<IComposite> GetChildren()
        {
            yield return _point;
        }
        private const float sortOverrideExtraDistance = 5000;
        public override void Click(Vector2 mousePosition, List<ClickHitData> clickHits,EventType eventType)
        {
            List<ClickHitData> pointHits = new List<ClickHitData>();
            _point.Click(mousePosition, pointHits,eventType);
            foreach (var i in pointHits)
                i.isLowPriority = true;
            clickHits.AddRange(pointHits);
        }

        public Vector3 Position { get { return _transformBlob.TransformPoint(_positionCurveComposite.PointClosestToCursor.position); } }
    }
}
