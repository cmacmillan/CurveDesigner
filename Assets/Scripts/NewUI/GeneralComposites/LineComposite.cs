﻿using System;
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
