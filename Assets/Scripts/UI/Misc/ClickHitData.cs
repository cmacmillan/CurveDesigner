using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public class ClickHitData
    {
        public float distanceFromCamera;
        public float distanceFromClick;
        public Clickable owner;
        public Vector2 offset;
        public bool hasBeenDragged=false;
        public bool isLowPriority = false;
        public ClickHitData(Clickable owner,float distanceFromCamera, Vector2 offset)
        {
            this.distanceFromCamera = distanceFromCamera;
            this.owner = owner;
            this.offset = offset;
        }
    }
}
