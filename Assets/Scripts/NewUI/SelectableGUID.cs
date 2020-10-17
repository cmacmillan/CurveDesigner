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
    public static bool Delete<T>(ref List<T> selectables, List<SelectableGUID> guids, Curve3D curve) where T : ISelectable
    {
        var newPoints = new List<T>();
        bool foundAPointToDelete = false;
        for (int i=0;i<selectables.Count;i++)
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
    public static List<SelectableGUID> SelectBetween(IActiveElement activeElement,SelectableGUID start,SelectableGUID end,Curve3D curve, BezierCurve curveConnectingPoints)
    {
        List<SelectableGUID> retr = new List<SelectableGUID>();
        if (start == end)//good ol' deselect
            return retr;
        int startIndex = -1;
        int endIndex = -1;
        ISelectable startSelectable=null;
        ISelectable endSelectable=null;
        ISelectable Get(int index)
        {
            return activeElement.GetSelectable(index, curve);
        }
        int count = activeElement.NumSelectables(curve);
        for (int i=0;i<count;i++)
        {
            var curr = Get(i);
            if (curr.GUID == start)
            {
                startIndex = i;
                startSelectable = curr;
            }
            else if (curr.GUID == end)
            {
                endIndex = i;
                endSelectable = curr;
            }
        }
        float startDistance = startSelectable.GetDistance(curveConnectingPoints);
        float endDistance = endSelectable.GetDistance(curveConnectingPoints);
        int sign = startDistance < endDistance ? 1 : -1;
        float directlyTowardsDistance = (endDistance - startDistance) * sign;
        float awayFromDistance = 0;
        if (sign == 1)
            awayFromDistance = (curveConnectingPoints.GetLength() - endDistance) + startDistance;
        else
            awayFromDistance = (curveConnectingPoints.GetLength() - startDistance) + endDistance;
        if (curveConnectingPoints.isClosedLoop && awayFromDistance < directlyTowardsDistance)
            sign *= -1;
        for (int i = startIndex; i != endIndex; i = mod(i+sign,count))
            retr.Add(Get(i).GUID);
        retr.Add(Get(endIndex).GUID);
        return retr;
    }
    static int mod(int x, int m)
    {
        return (x % m + m) % m;
    }
}
public static class ListSelectableGUIDExtension
{
    public static List<T> GetSelected<T>(this List<SelectableGUID> selectionPoints,IEnumerable<T> points) where T : ISelectable
    {
        List<T> retr = new List<T>();
        foreach (var i in points)
            if (selectionPoints.Contains(i.GUID))
                    retr.Add(i);
        return retr;
    }
}

//where need to more properly handle when we change contexts
public interface ISelectable
{
    SelectableGUID GUID { get; }
    float GetDistance(BezierCurve positionCurve);
    bool IsInsideVisibleCurve(BezierCurve curve);
}
public interface ISelectEditable<T> : ISelectable
{
    void SelectEdit(Curve3D curve,List<T> selectedPoints);
}
//Active elements can have stuff deleted from them and have all their elements selected
public interface IActiveElement
{
    string GetPointName();
    ISelectable GetSelectable(int index,Curve3D curve);
    int NumSelectables(Curve3D curve);
    bool Delete(List<SelectableGUID> guids,Curve3D curve);
    List<SelectableGUID> SelectAll(Curve3D curve);
}
