using System.Collections.Generic;

namespace ChaseMacMillan.CurveDesigner
{
    //Active elements can have stuff deleted from them and have all their elements selected
    public interface IActiveElement
    {
        string GetPointName();
        ISelectable GetSelectable(int index, Curve3D curve);
        int NumSelectables(Curve3D curve);
        bool Delete(List<SelectableGUID> guids, Curve3D curve);
        List<SelectableGUID> SelectAll(Curve3D curve);
    }
}
