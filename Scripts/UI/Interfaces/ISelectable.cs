using System.Collections.Generic;

namespace ChaseMacMillan.CurveDesigner
{
    public interface ISelectable
    {
        SelectableGUID GUID { get; }
        float GetDistance(BezierCurve positionCurve);
        bool IsInsideVisibleCurve(BezierCurve curve);
    }
    public interface ISelectEditable<T> : ISelectable
    {
#if UNITY_EDITOR
        void SelectEdit(Curve3D curve, List<T> selectedPoints);
#endif
    }
}
