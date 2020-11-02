using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public interface IPosition : IPositionSetter, IPositionProvider { }
    public interface IPositionSetter
    {
        void SetPosition(Vector3 position,List<SelectableGUID> selected);
    }
    public interface IPositionProvider
    {
        Vector3 Position { get; }
    }
    public class PointGroupPointPositionProvider : IPosition
    {
        private PointGroup _group;
        private PGIndex _type;
        private TransformBlob transformBlob;
        private BezierCurve _positionCurve;
        private Curve3D _curve;
        public PointGroupPointPositionProvider(PointGroup group,PGIndex type, TransformBlob transformBlob, BezierCurve positionCurve,Curve3D curve)
        {
            this.transformBlob = transformBlob;
            _group = group;
            _type = type;
            _positionCurve = positionCurve;
            _curve = curve;
        }
        public Vector3 Position {
            get {
                return transformBlob.TransformPoint(_group.GetWorldPositionByIndex(_type,_positionCurve.dimensionLockMode));
            }
        }
        public void SetPosition(Vector3 position,List<SelectableGUID> selected)
        {
            var dimensionLockMode = _positionCurve.dimensionLockMode;
            Vector3 newPointPosition = transformBlob.InverseTransformPoint(position);
            Vector3 oldPointPosition = _group.GetWorldPositionByIndex(_type,dimensionLockMode);
            Vector3 pointOffset = newPointPosition - oldPointPosition;
            foreach (var i in _curve.GetSelected<PointGroup>(selected))
            {
                i.SetWorldPositionByIndex(_type, i.GetWorldPositionByIndex(_type, dimensionLockMode) + pointOffset, dimensionLockMode);
            }
        }
    }
}
