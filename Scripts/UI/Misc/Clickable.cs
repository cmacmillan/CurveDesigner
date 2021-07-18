using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public abstract class Clickable : Composite
    {
        public Clickable(Composite parent) : base(parent) { }

        public abstract float DistanceFromMouse(Vector2 mouse);
       
        public abstract IClickCommand GetClickCommand();

        public bool IsSelectable { get { return GUID == SelectableGUID.Null; } }
    }
}
