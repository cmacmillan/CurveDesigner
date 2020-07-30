using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
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
        protected bool useExtendedBounds;
        public float distanceFromBounds;

        public IClickable(IComposite parent,bool useExtendedBounds) : base(parent){
            this.useExtendedBounds = useExtendedBounds;
        }

        public bool IsWithinBounds(Vector2 point)
        {
            if (!TryGetBounds(out Rect bounds))
            {
                distanceFromBounds = float.MaxValue;
                return false;
            }
            if (bounds.Contains(point))
            {
                distanceFromBounds = 0;
                return true;
            }
            if (!useExtendedBounds)
                distanceFromBounds = float.MaxValue;
            else
            {
                float xOffset;
                if (point.x > bounds.xMax)
                    xOffset = point.x - bounds.xMax;
                else
                    xOffset = bounds.xMin - point.x;

                float yOffset;
                if (point.y > bounds.yMax)
                    yOffset = point.y - bounds.yMax;
                else
                    yOffset = bounds.yMin - point.y;
                distanceFromBounds = new Vector2(xOffset, yOffset).magnitude;
            }
            return false;
        }

        protected abstract bool TryGetBounds(out Rect bounds);

        public abstract IClickCommand GetClickCommand();

        public bool IsSelectable { get { return Guid == SelectableGUID.Null; } }

        public abstract SelectableGUID Guid { get; }
    }
}
