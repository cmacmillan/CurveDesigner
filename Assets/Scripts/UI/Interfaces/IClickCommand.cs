using System.Collections.Generic;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    /// <summary>
    // An interface for the command pattern
    /// </summary>
    public interface IClickCommand
    {
        void ClickDown(Vector2 mousePos, Curve3D curve,List<SelectableGUID> selected);
        void ClickDrag(Vector2 mousePos,Curve3D curve,ClickHitData clicked,List<SelectableGUID> selected);
        void ClickUp(Vector2 mousePos,Curve3D curve,List<SelectableGUID> selected);
    }
}
