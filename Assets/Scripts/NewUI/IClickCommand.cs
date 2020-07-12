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
    public abstract class IClickable : IComposite
    {
        public IClickable(IComposite parent) : base(parent){}

        public abstract IClickCommand GetClickCommand();    

        public bool IsSelectable { get { return Guid == SelectableGUID.Null; } }

        public abstract SelectableGUID Guid { get; }
    }
}
