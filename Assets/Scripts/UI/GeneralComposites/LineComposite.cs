using System.Collections.Generic;

namespace ChaseMacMillan.CurveDesigner
{
    public class LineComposite : IComposite
    {
        private IPositionProvider _start;
        private IPositionProvider _end;
        public LineComposite(IComposite parent, IPositionProvider start, IPositionProvider end) : base(parent)
        {
            this._start = start;
            this._end = end;
        }

        public override void Draw(List<IDraw> drawList,ClickHitData closestElementToCursor)
        {
            drawList.Add(new LineDraw(this,_start.Position,_end.Position));
        }
    }
}
