using System.Collections.Generic;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public class DoNothingClickCommand : IClickCommand
    {
        public void ClickDown(Vector2 mousePos, Curve3D curve, List<SelectableGUID> selected) { }
        public void ClickDrag(Vector2 mousePos, Curve3D curve, ClickHitData clicked, List<SelectableGUID> selected) { }
        public void ClickUp(Vector2 mousePos, Curve3D curve, List<SelectableGUID> selected) { }
    }
}
