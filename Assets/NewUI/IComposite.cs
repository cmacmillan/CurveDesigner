using System;
using System.Collections.Generic;
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
        public virtual void Draw(List<IDraw> drawList)
        {
            foreach (var i in GetChildren())
                i.Draw(drawList);
        }
        public abstract IEnumerable<IComposite> GetChildren();
        public abstract ClickHitData Click(Vector2 position);
    }
}
