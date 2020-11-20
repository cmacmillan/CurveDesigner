using System.Collections.Generic;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
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
        private PointGroupIndex _type;
        private TransformBlob transformBlob;
        private BezierCurve _positionCurve;
        private Curve3D _curve;
        public PointGroupPointPositionProvider(PointGroup group,PointGroupIndex type, TransformBlob transformBlob, BezierCurve positionCurve,Curve3D curve)
        {
            this.transformBlob = transformBlob;
            _group = group;
            _type = type;
            _positionCurve = positionCurve;
            _curve = curve;
        }
        public Vector3 Position {
            get {
                return transformBlob.TransformPoint(_group.GetWorldPositionByIndex(_type));
            }
        }
        public void SetPosition(Vector3 position,List<SelectableGUID> selected)
        {
            var dimensionLockMode = _positionCurve.dimensionLockMode;
            Vector3 newPointPosition = transformBlob.InverseTransformPoint(position);
            Vector3 oldPointPosition = _group.GetWorldPositionByIndex(_type);
            Vector3 pointOffset = newPointPosition - oldPointPosition;
            Dictionary<BezierCurve, SegmentIndexSet> curvesToRecalculate = new Dictionary<BezierCurve, SegmentIndexSet>();//yeesh allocations. Too lazy to use lists here tho
            foreach (var i in _curve.GetSelected<PointGroup>(selected))
            {
                i.SetWorldPositionByIndex(_type, i.GetWorldPositionByIndex(_type) + pointOffset);
                if (!curvesToRecalculate.ContainsKey(i.owner))
                {
                    curvesToRecalculate.Add(i.owner, new SegmentIndexSet(i.owner));
                }
                curvesToRecalculate[i.owner].Add(i.segmentIndex);
            }
            foreach (var i in curvesToRecalculate)
            {
                i.Key.Recalculate(null, i.Value);
            }
        }
    }
}
