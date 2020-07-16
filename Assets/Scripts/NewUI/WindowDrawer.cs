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
        public static void Draw<T>(IEnumerable<T> selectables,Curve3D curve) where T : ISelectEditable<T>
        {
            List<SelectableGUID> selectedPoints = curve.selectedPoints;
            if (selectedPoints.Count == 0)
                return;
            var primaryPointIndex = selectedPoints.First();
            var primaryPoint = selectables.Where(a=>a.GUID==selectedPoints[0]).FirstOrDefault();
            if (primaryPoint == null)
                return;
            EditorGUI.BeginChangeCheck();
            List<T> selected = new List<T>();
            foreach (var j in selectables)
                if (selectedPoints.Contains(j.GUID))
                    selected.Add(j);
            primaryPoint.SelectEdit(curve, selected);
            if (EditorGUI.EndChangeCheck())
            {
                curve.RequestMeshUpdate();
            }
        }
    }
}
