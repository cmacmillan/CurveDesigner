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
}
public interface ISelectable
{
    SelectableGUID GUID { get; }
    bool SelectEdit(Curve3D curve,out IMultiEditOffsetModification offsetMod);
}
