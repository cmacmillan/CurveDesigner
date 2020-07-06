using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.NewUI
{
    public interface IWindowDrawer
    {
        void DrawWindow(Curve3D curve);
    }
    public static class WindowDrawer
    {
        public static void Draw(IEnumerable<ISelectable> selectables,Curve3D curve)
        {
            List<SelectableGUID> selectedPoints = curve.selectedPoints;
            if (selectedPoints.Count == 0)
                return;
            var primaryPointIndex = selectedPoints.First();
            var primaryPoint = selectables.Where(a=>a.GUID==selectedPoints[0]).FirstOrDefault();
            if (primaryPoint == null)
                return;
            EditorGUI.BeginChangeCheck();
            if (primaryPoint.SelectEdit(curve,out IMultiEditOffsetModification offsetMod))
            {
                foreach (var i in selectedPoints)
                    foreach (var j in selectables)
                        if (i == j.GUID)
                        {
                            offsetMod.Apply(j, curve);
                            break;
                        }
            }
            if (EditorGUI.EndChangeCheck())
            {
                curve.RequestMeshUpdate();
            }           
        }
    }
}
