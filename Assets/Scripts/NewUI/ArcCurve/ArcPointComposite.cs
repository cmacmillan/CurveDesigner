using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.NewUI
{
    public class ArcPointComposite : IComposite
    {
        public ArcPointComposite(IComposite parent) : base(parent)
        {

        }
    }
}
