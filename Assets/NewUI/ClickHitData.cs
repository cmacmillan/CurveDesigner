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
        public bool isLowPriority;
        public IClickable owner;
        public Vector2 offset;
        public ClickHitData(IClickable owner,float distanceFromClick, float distanceFromCamera, Vector2 offset, bool isLowPriority=false)
        {
            this.isLowPriority = isLowPriority;
            this.distanceFromClick = distanceFromClick;
            this.distanceFromCamera = distanceFromCamera;
            this.owner = owner;
            this.offset = offset;
        }
    }
}
