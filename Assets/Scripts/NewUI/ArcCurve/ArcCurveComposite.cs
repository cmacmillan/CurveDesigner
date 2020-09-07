using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.NewUI
{
    public class ArcCurveComposite : IComposite, IValueAlongCurvePointProvider, IWindowDrawer
    {
        public ArcCurveComposite(IComposite parent) : base(parent)
        {

        }
        public void DrawWindow(Curve3D curve)
        {
            throw new NotImplementedException();
        }

        public IClickable GetPointAtIndex(int index)
        {
            throw new NotImplementedException();
        }
    }
}
