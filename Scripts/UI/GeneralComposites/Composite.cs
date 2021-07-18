using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    /// <summary>
    /// An abstract class implementing the Composite pattern for ui elements
    /// </summary>
    public abstract class Composite
    {
        public Composite(Composite parent)
        {
            _parent = parent;
        }
        protected Composite _parent=null;
        public virtual void Draw(List<IDraw> drawList,ClickHitData closestElementToCursor)
        {
            foreach (var i in GetChildren())
                i.Draw(drawList,closestElementToCursor);
        }
        public virtual IEnumerable<Composite> GetChildren()
        {
            return Enumerable.Empty<Composite>();
        }
        public virtual SelectableGUID GUID { get { return _parent.GUID; } }
        public Composite GetParent()
        {
            return _parent;
        }
        public virtual void Click(Vector2 mousePosition,List<ClickHitData> clickHits)
        {
            foreach (var i in GetChildren())
                i.Click(mousePosition, clickHits);
        }
    }
}
