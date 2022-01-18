using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    [System.Serializable]
    public class SelectableGUIDFactory
    {
        [SerializeField]
        private int currentGUID = 0;
        public SelectableGUID GetGUID(ISelectable selectable)
        {
            currentGUID++;
            var newGuid = new SelectableGUID(currentGUID);
            Objects.Add(newGuid, selectable);
            return newGuid;
        }
        [NonSerialized]
        public Dictionary<SelectableGUID, ISelectable> Objects = new Dictionary<SelectableGUID, ISelectable>();//we need to rebuild this every time we deserialize
        public IEnumerable<T> GetSelected<T>(List<SelectableGUID> selected) where T : class, ISelectable
        {
            foreach (var i in selected)
            {
                if (Objects.TryGetValue(i, out ISelectable value))
                {
                    var casted = value as T;
                    if (casted != null)
                        yield return casted;
                }
            }
        }
        public IEnumerable<ISelectable> GetSelected(List<SelectableGUID> selected)
        {
            foreach (var i in selected)
            {
                if (Objects.TryGetValue(i, out ISelectable value))
                    yield return value;
            }
        }
    }
    //just a typed int
    [System.Serializable]
    public struct SelectableGUID
    {
        public static SelectableGUID Null = new SelectableGUID(-1);
        public SelectableGUID(int id)
        {
            this.id = id;
        }
        [SerializeField]
        private int id;
        public static bool operator ==(SelectableGUID g1, SelectableGUID g2)
        {
            return g1.id == g2.id;
        }
        public static bool operator !=(SelectableGUID g1, SelectableGUID g2)
        {
            return g1.id != g2.id;
        }
        public override int GetHashCode()
        {
            return id.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            return obj.Equals(id);
        }
        public static bool Delete<T>(ref List<T> selectables, List<SelectableGUID> guids, Curve3D curve) where T : ISelectable
        {
            var newPoints = new List<T>();
            bool foundAPointToDelete = false;
            for (int i = 0; i < selectables.Count; i++)
            {
                var curr = selectables[i];
                if (guids.Contains(curr.GUID))
                    foundAPointToDelete = true;
                else
                    newPoints.Add(curr);
            }
            selectables = newPoints;
            return foundAPointToDelete;
        }
    }
    public static class ListSelectableGUIDExtension
    {
        public static List<T> GetSelected<T>(this List<SelectableGUID> selectionPoints, IEnumerable<T> points) where T : ISelectable
        {
            List<T> retr = new List<T>();
            foreach (var i in points)
                if (selectionPoints.Contains(i.GUID))
                    retr.Add(i);
            return retr;
        }
    }
}
