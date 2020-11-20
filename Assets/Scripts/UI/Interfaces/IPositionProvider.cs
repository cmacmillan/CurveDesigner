using System.Collections.Generic;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public interface IPosition : IPositionSetter, IPositionProvider { }
    public interface IPositionSetter
    {
        void SetPosition(Vector3 position,List<SelectableGUID> selected);
    }
    public interface IPositionProvider
    {
        Vector3 Position { get; }
    }
}
