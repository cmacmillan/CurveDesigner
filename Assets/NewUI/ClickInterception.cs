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
        public IComposite owner;
        public ClickHitData(IComposite owner,float distanceFromClick, float distanceFromCamera,IClickCommand command)
        {
            this.distanceFromClick = distanceFromClick;
            this.distanceFromCamera = distanceFromCamera;
            this.commandToExecute = command;
            this.owner = owner;
        }
    }
}
