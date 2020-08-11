using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class ClickHitData
    {
        public float DistanceFromMouse(Vector2 mousePos)
        {
            return owner.DistanceFromMouse(mousePos);
        }
        public float distanceFromCamera;
        public IClickable owner;
        public Vector2 offset;
        public bool hasBeenDragged=false;
        public bool isLowPriority = false;
        public ClickHitData(IClickable owner,float distanceFromCamera, Vector2 offset)
        {
            this.distanceFromCamera = distanceFromCamera;
            this.owner = owner;
            this.offset = offset;
        }
    }
}
