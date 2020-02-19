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
        void ClickDrag(Vector2 mousePos,Curve3D curve,ClickHitData data);
        void ClickUp(Vector2 mousePos);
    }
}
