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
        void ClickDown(Vector2 mousePos);
        void ClickDrag(Vector2 mousePos,Curve3D curve,ClickHitData clicked);
        void ClickUp(Vector2 mousePos);
    }
    public abstract class IClickable : IComposite
    {
        public IClickable(IComposite parent) : base(parent){}

        public abstract IClickCommand GetClickCommand();
    }
}
