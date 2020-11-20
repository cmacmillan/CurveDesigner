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
    public class DoNothingClickCommand : IClickCommand
    {
        public void ClickDown(Vector2 mousePos, Curve3D curve, List<SelectableGUID> selected) { }
        public void ClickDrag(Vector2 mousePos, Curve3D curve, ClickHitData clicked, List<SelectableGUID> selected) { }
        public void ClickUp(Vector2 mousePos, Curve3D curve, List<SelectableGUID> selected) { }
    }
    public abstract class IClickable : IComposite
    {
        public IClickable(IComposite parent) : base(parent) { }

        public abstract float DistanceFromMouse(Vector2 mouse);
       
        public abstract IClickCommand GetClickCommand();

        public bool IsSelectable { get { return GUID == SelectableGUID.Null; } }

    }
}
