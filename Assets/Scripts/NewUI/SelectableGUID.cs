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
    public static List<SelectableGUID> SelectBetween(IActiveElement activeElement,SelectableGUID previous,SelectableGUID next,Curve3D curve)
    {
        List<SelectableGUID> retr = new List<SelectableGUID>();
        if (previous == next)//good ol' deselect
            return retr;
        int firstIndex = -1;
        int lastIndex = -1;

        SelectableGUID Get(int index)
        {
            return activeElement.GetSelectable(index, curve).GUID;
        }
        int count = activeElement.NumSelectables(curve);

        for (int i=0;i<count;i++)
        {
            var curr = Get(i);
            if (curr== previous)
                firstIndex = i;
            else if (curr== next)
                lastIndex = i;
        }
        if (firstIndex==-1)
        {
            retr.Add(next);
            return retr;
        }
        List<SelectableGUID> left = new List<SelectableGUID>();
        List<SelectableGUID> right = new List<SelectableGUID>();
        for (int i = firstIndex; i != lastIndex; i = mod(i - 1, count))
            left.Add(Get(i));
        left.Add(next);
        for (int i = firstIndex; i != lastIndex; i = mod(i + 1, count))
            right.Add(Get(i));
        right.Add(next);
        if (left.Count < right.Count)
            return left;
        return right;
    }
    static int mod(int x, int m)
    {
        return (x % m + m) % m;
    }
}
public interface ISelectable
{
    SelectableGUID GUID { get; }
    bool SelectEdit(Curve3D curve,out IMultiEditOffsetModification offsetMod);
    float distanceAlongCurve(Curve3D curve);
}
//Active elements can have stuff deleted from them and have all their elements selected
public interface IActiveElement
{
    ISelectable GetSelectable(int index,Curve3D curve);
    int NumSelectables(Curve3D curve);
    bool Delete(List<SelectableGUID> guids,Curve3D curve);
    List<SelectableGUID> SelectAll(Curve3D curve);
}
