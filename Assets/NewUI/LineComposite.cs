using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.NewUI
{
    public class LineComposite : IComposite
    {
        private IPositionProvider _start;
        private IPositionProvider _end;
        public LineComposite(IPositionProvider start, IPositionProvider end)
        {
            this._start = start;
            this._end = end;
        }

        public override void Draw(List<IDraw> drawList)
        {
            drawList.Add(new LineDraw(_start.Position,_end.Position));
        }
    }
}
