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
        private Curve3D _curve;
        private const float _maxSplitClickDistance = 10;
        public SplitterPointComposite(IComposite parent,Curve3D _curve,PointTextureType textureType,ISplitCommandFactory commandFactory,Color color) : base (parent)
        {
            this._curve = _curve;
            this._point = new PointComposite(this,this,textureType,commandFactory.Create(this,_curve),color);
        }
        public override void Draw(List<IDraw> drawlist,ClickHitData clickedElement)
        {
            if (clickedElement != null && clickedElement.owner != null && clickedElement.owner.GetParent() == this && clickedElement.distanceFromClick < _maxSplitClickDistance)
                base.Draw(drawlist,clickedElement);
        }
        public override IEnumerable<IComposite> GetChildren()
        {
            yield return _point;
        }
        private const float sortOverrideExtraDistance = 5000;
        public override void Click(Vector2 mousePosition, List<ClickHitData> clickHits)
        {
            List<ClickHitData> pointHits = new List<ClickHitData>();
            _point.Click(mousePosition, clickHits);
            foreach (var i in clickHits)
                i.isLowPriority = true;
            clickHits.AddRange(pointHits);
        }
        public Vector3 Position { get { return _curve.transform.TransformPoint(_curve.UICurve.pointClosestToCursor.position); } }
    }
}
