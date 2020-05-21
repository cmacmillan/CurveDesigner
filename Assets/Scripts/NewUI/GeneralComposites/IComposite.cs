using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
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
        public virtual void Draw(List<IDraw> drawList,ClickHitData clickedElement)
        {
            foreach (var i in GetChildren())
                i.Draw(drawList,clickedElement);
        }
        public virtual IEnumerable<IComposite> GetChildren()
        {
            return Enumerable.Empty<IComposite>();
        }
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
