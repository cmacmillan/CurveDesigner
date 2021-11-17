#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public class SplitterPointComposite : Composite, IPositionProvider
    {
        private PointComposite _point;
        private TransformBlob _transformBlob;
        private const float _maxSplitClickDistance = 10;
        private PositionCurveComposite _positionCurveComposite;
        public SplitterPointComposite(Composite parent,TransformBlob transformBlob,PointTextureType textureType,IClickCommand clickCommand,Color color,PositionCurveComposite positionCurveComposite) : base (parent)
        {
            _positionCurveComposite = positionCurveComposite;
            _transformBlob = transformBlob;
            _point = new PointComposite(this,this,textureType,clickCommand,color,SelectableGUID.Null,true);
        }
        public override IEnumerable<Composite> GetChildren()
        {
            yield return _point;
        }
        public override void Click(Vector2 mousePosition, List<ClickHitData> clickHits)
        {
            List<ClickHitData> pointHits = new List<ClickHitData>();
            _point.Click(mousePosition, pointHits);
            foreach (var i in pointHits)
                i.isLowPriority = true;
            clickHits.AddRange(pointHits);
        }

        public Vector3 Position { get { return _transformBlob.TransformPoint(_positionCurveComposite.PointClosestToCursor.position); } }
    }
}
#endif
