using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class ClickHitData
    {
        public float distanceFromClick;
        public float distanceFromCamera;
        public IClickCommand commandToExecute;
        public IComposite owner;
        public Vector2 offset;
        public ClickHitData(IComposite owner,float distanceFromClick, float distanceFromCamera,IClickCommand command, Vector2 offset)
        {
            this.distanceFromClick = distanceFromClick;
            this.distanceFromCamera = distanceFromCamera;
            this.commandToExecute = command;
            this.owner = owner;
            this.offset = offset;
        }
    }
}
