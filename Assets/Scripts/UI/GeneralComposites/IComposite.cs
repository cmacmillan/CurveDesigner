using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    /// <summary>
    /// An abstract class implementing the Composite pattern for ui elements
    /// </summary>
    public abstract class IComposite
    {
        public IComposite(IComposite parent)
        {
            _parent = parent;
        }
        protected IComposite _parent=null;
        public virtual void Draw(List<IDraw> drawList,ClickHitData closestElementToCursor)
        {
            foreach (var i in GetChildren())
                i.Draw(drawList,closestElementToCursor);
        }
        public virtual IEnumerable<IComposite> GetChildren()
        {
            return Enumerable.Empty<IComposite>();
        }
        public virtual SelectableGUID GUID { get { return _parent.GUID; } }
        public IComposite GetParent()
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
