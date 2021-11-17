#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
namespace ChaseMacMillan.CurveDesigner
{
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
                return transformBlob.TransformPoint(_group.GetPositionLocal(_type));
            }
        }
        public void SetPosition(Vector3 position,List<SelectableGUID> selected)
        {
            var dimensionLockMode = _positionCurve.dimensionLockMode;
            Vector3 newPointPosition = transformBlob.InverseTransformPoint(position);
            Vector3 oldPointPosition = _group.GetPositionLocal(_type);
            Vector3 pointOffset = newPointPosition - oldPointPosition;
            Dictionary<BezierCurve, HashSet<int>> curvesToRecalculate = new Dictionary<BezierCurve, HashSet<int>>();//yeesh allocations. Too lazy to use lists here tho
            foreach (var i in _curve.GetSelected<PointGroup>(selected))
            {
                i.SetPositionLocal(_type, i.GetPositionLocal(_type) + pointOffset);
                if (!curvesToRecalculate.ContainsKey(i.owner))
                {
                    curvesToRecalculate.Add(i.owner, new HashSet<int>());
                }
                PositionPointClickCommand.AddIndexToRecalculate(curvesToRecalculate[i.owner], i.segmentIndex, i.owner, i.owner.automaticTangents);
            }
            foreach (var i in curvesToRecalculate)
            {
                i.Key.Recalculate(null, i.Value);
            }
        }
    }
}
#endif
