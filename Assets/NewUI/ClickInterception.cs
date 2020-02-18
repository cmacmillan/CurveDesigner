using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Assets.NewUI
{
    public class ClickHitData
    {
        public float distanceFromClick;
        public float distanceFromCamera;
        public IClickCommand commandToExecute;
    }
}
