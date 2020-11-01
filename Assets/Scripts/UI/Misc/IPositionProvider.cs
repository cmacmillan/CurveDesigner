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
        public PointGroupPointPositionProvider(PointGroup group,PGIndex type, TransformBlob transformBlob, BezierCurve positionCurve)
        {
            this.transformBlob = transformBlob;
            _group = group;
            _type = type;
            _positionCurve = positionCurve;
        }
        public Vector3 Position {
            get {
                return transformBlob.TransformPoint(_group.GetWorldPositionByIndex(_type,_positionCurve.dimensionLockMode));
            }
        }
        public void SetPosition(Vector3 position,List<SelectableGUID> selected)
        {
            //we need to write a GetSelected<T>(List<SelectableGUID> selected) method
            //And we build the datastruct on ui rebuild?


            Vector3 newPointPosition = transformBlob.InverseTransformPoint(position);
            Vector3 pointOffset = newPointPosition - oldPointPosition;
            List<PointGroup> selectedPointGroups = new List<PointGroup>();
            foreach (var i in allCurves)
                foreach (var j in i.PointGroups)
                    if (selected.Contains(j.GUID))
                        selectedPointGroups.Add(j);
            foreach (var i in selectedPointGroups)
                i.SetWorldPositionByIndex(_index, i.GetWorldPositionByIndex(_index, dimensionLockMode) + pointOffset, dimensionLockMode);
            //_group.SetWorldPositionByIndex(_type, ,_positionCurve.dimensionLockMode);
        }
    }
}
