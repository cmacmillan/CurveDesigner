using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public interface IPositionProvider
    {
        Vector3 Position { get; set; }
    }
    public class PointGroupPointPositionProvider : IPositionProvider
    {
        private PointGroup _group;
        private PGIndex _type;
        public PointGroupPointPositionProvider(PointGroup group,PGIndex type)
        {
            _group = group;
            _type = type;
        }
        public Vector3 Position {
            get { return _group.GetWorldPositionByIndex(_type); }
            set { _group.SetWorldPositionByIndex(_type,value); }
        }
    }
}
