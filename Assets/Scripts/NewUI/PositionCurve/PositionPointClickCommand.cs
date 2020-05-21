using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class PositionPointClickCommand : IClickCommand
    {
        private PointGroup _group;
        private PGIndex _index;
        public PositionPointClickCommand(PointGroup group,PGIndex indexType)
        {
            this._group = group;
            this._index = indexType;
        }
        public void ClickDown(Vector2 mousePos)
        {
        }

        public void ClickDrag(Vector2 mousePos,Curve3D curve,ClickHitData data)
        {
            var oldPointPosition = _group.GetWorldPositionByIndex(_index);
            var newPointPosition = curve.transform.InverseTransformPoint(GUITools.GUIToWorldSpace(mousePos,data.distanceFromCamera));
            _group.SetWorldPositionByIndex(_index,newPointPosition);
        }

        public void ClickUp(Vector2 mousePos)
        {
        }
    }
}
