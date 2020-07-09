using Assets.NewUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[System.Serializable]
public class SelectableGUIDFactory
{
    [SerializeField]
    private int currentGUID=0;
    public SelectableGUID GetGUID()
    {
        currentGUID++;
        return new SelectableGUID(currentGUID);
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
    public static bool Delete<T>(ref List<T> selectables, List<SelectableGUID> guids, Curve3D curve,int minCount=-1) where T : ISelectable
    {
        var newPoints = new List<T>();
        bool foundAPointToDelete = false;
        for (int i=0;i<selectables.Count;i++)
        {
            int remaining = selectables.Count - i;
            var curr = selectables[i];
            if (guids.Contains(curr.GUID) && (minCount==-1 || newPoints.Count+remaining>minCount))
                foundAPointToDelete = true;
            else
                newPoints.Add(curr);
        }
        selectables = newPoints;
        return foundAPointToDelete;
    }
}
public interface ISelectable
{
    SelectableGUID GUID { get; }
    bool SelectEdit(Curve3D curve,out IMultiEditOffsetModification offsetMod);
}
//Active elements can have stuff deleted from them and have all their elements selected
public interface IActiveElement
{
    bool Delete(List<SelectableGUID> guids,Curve3D curve);
    List<SelectableGUID> SelectAll(Curve3D curve);
}
